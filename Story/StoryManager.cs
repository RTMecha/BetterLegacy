using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Menus;

namespace BetterLegacy.Story
{
    /// <summary>
    /// Manager class for handling the BetterLegacy (Classic Arrhythmia) story mode.
    /// </summary>
    public class StoryManager : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="StoryManager"/> global instance reference.
        /// </summary>
        public static StoryManager inst;

        /// <summary>
        /// Initializes <see cref="StoryManager"/>.
        /// </summary>
        public static void Init() => new GameObject(nameof(StoryManager), typeof(StoryManager)).transform.SetParent(SystemManager.inst.transform);

        void Awake()
        {
            inst = this;
            StoryMode.Init();
        }

        /// <summary>
        /// Path to the story assets folder.
        /// </summary>
        public static string StoryAssetsPath => RTFile.GetAsset("Story/");

        /// <summary>
        /// If a story level was loaded.
        /// </summary>
        public bool Loaded { get; set; }

        /// <summary>
        /// If story should continue when a level is completed.
        /// </summary>
        public bool ContinueStory { get; set; } = true;

        /// <summary>
        /// Story mode reference for debugging
        /// </summary>
        public static StoryMode StoryModeDebugRef => StoryMode.Instance;

        #endregion

        #region Progression

        /// <summary>
        /// The default chapter rank requirement for "bonuses" to be unlocked. In this case, the player needs to get higher than a B rank (S / SS / A rank).
        /// </summary>
        public const int CHAPTER_RANK_REQUIREMENT = 3;

        public int currentPlayingChapterIndex;
        public int currentPlayingLevelSequenceIndex;
        public bool inBonusChapter;

        /// <summary>
        /// The story mode chapter that is currently open.
        /// </summary>
        public StoryMode.Chapter CurrentChapter => StoryMode.Instance.chapters[currentPlayingChapterIndex];
        /// <summary>
        /// The story mode level that is selected.
        /// </summary>
        public StoryMode.LevelSequence CurrentLevelSequence => currentPlayingLevelSequenceIndex < CurrentChapter.Count ? CurrentChapter[currentPlayingLevelSequenceIndex] : CurrentChapter.transition.levelSequence;

        StorySave currentSaveSlot = new StorySave();
        /// <summary>
        /// The current story save.
        /// </summary>
        public StorySave CurrentSave
        {
            get
            {
                // save slot should not be null.
                if (!currentSaveSlot)
                    currentSaveSlot = new StorySave();
                return currentSaveSlot;
            }
            set
            {
                // cannot set null
                if (value)
                    currentSaveSlot = value;
            }
        }

        #endregion

        #region Play

        #region Resource

        void Update()
        {
            if (CoreHelper.IsUsingInputField)
                return;

            var key = CoreHelper.GetKeyCodeDown();

            if (key == KeyCode.None)
                return;

            for (int i = 0; i < secretSequences.Count; i++)
                secretSequences[i].Tick(key);
        }

        List<SecretSequence> secretSequences = new List<SecretSequence>()
        {
            new SecretSequence(new List<KeyCode> { KeyCode.B, KeyCode.E, KeyCode.L, KeyCode.U, KeyCode.G, KeyCode.A, }, () =>
            {
                SoundManager.inst.PlaySound(inst.gameObject, DefaultSounds.loadsound);

                if (Editor.Managers.RTEditor.inst)
                {
                    Editor.Managers.RTEditor.inst.ShowWarningPopup("Are you sure you want to continue?", () =>
                    {
                        AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                        LoadResourceLevel(true, SAVE_RESOURCE, SAVE_RESOURCE);
                    }, Editor.Managers.RTEditor.inst.HideWarningPopup);
                    return;
                }

                AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                LoadResourceLevel(true, SAVE_RESOURCE, SAVE_RESOURCE);
            }), // load save
            new SecretSequence(new List<KeyCode> { KeyCode.D, KeyCode.E, KeyCode.M, KeyCode.O, }, () =>
            {
                SoundManager.inst.PlaySound(inst.gameObject, DefaultSounds.loadsound);

                if (Editor.Managers.RTEditor.inst)
                {
                    Editor.Managers.RTEditor.inst.ShowWarningPopup("Are you sure you want to continue?", () =>
                    {
                        AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                        LoadResourceLevel(false, AOTC_RESOURCE, AOTC_RESOURCE);
                    }, Editor.Managers.RTEditor.inst.HideWarningPopup);
                    return;
                }

                AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                LoadResourceLevel(false, AOTC_RESOURCE, AOTC_RESOURCE);
            }), // load old demo
            new SecretSequence(new List<KeyCode> { KeyCode.M, KeyCode.I, KeyCode.K, KeyCode.U, }, () =>
            {
                SoundManager.inst.PlaySound(inst.gameObject, DefaultSounds.loadsound);

                if (Editor.Managers.RTEditor.inst)
                {
                    Editor.Managers.RTEditor.inst.ShowWarningPopup("Are you sure you want to continue?", () =>
                    {
                        AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                        LoadResourceLevel(false, VIDEO_TEST_LEVEL, VIDEO_TEST_MUSIC, VIDEO_TEST_VIDEO);
                    }, Editor.Managers.RTEditor.inst.HideWarningPopup);
                    return;
                }

                AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                LoadResourceLevel(false, VIDEO_TEST_LEVEL, VIDEO_TEST_MUSIC, VIDEO_TEST_VIDEO);
            }), // load old demo
        };

        class SecretSequence
        {
            public SecretSequence(List<KeyCode> keys, Action onSequenceEnd)
            {
                this.keys = keys;
                this.onSequenceEnd = onSequenceEnd;
            }

            int counter;

            /// <summary>
            /// Current sequence count.
            /// </summary>
            public int Counter => counter;

            /// <summary>
            /// Sequence of keys to type to run the function.
            /// </summary>
            public List<KeyCode> keys;

            /// <summary>
            /// Function to run when the typing sequence is done.
            /// </summary>
            public Action onSequenceEnd;

            /// <summary>
            /// Ticks the sequence.
            /// </summary>
            /// <param name="key">Input key.</param>
            public void Tick(KeyCode key)
            {
                KeyCode keyCompare = KeyCode.None;
                if (counter < keys.Count)
                    keyCompare = keys[counter];

                if (key == keyCompare)
                    counter++;
                else
                    counter = 0;

                if (counter == keys.Count)
                {
                    onSequenceEnd?.Invoke();
                    counter = 0;
                }
            }
        }

        public static void LoadResourceLevel(int type)
        {
            switch (type)
            {
                case 0: // save
                    {
                        LoadResourceLevel(true, SAVE_RESOURCE, SAVE_RESOURCE);
                        break;
                    }
                case 1: // ahead of the curve
                    {
                        LoadResourceLevel(false, AOTC_RESOURCE, AOTC_RESOURCE);
                        break;
                    }
                case 2: // new
                    {
                        LoadResourceLevel(false, NEW_RESOURCE, NEW_RESOURCE);
                        break;
                    }
                case 3: // node
                    {
                        LoadResourceLevel(true, NODE_RESOURCE, NODE_RESOURCE);
                        break;
                    }
                case 4: // video test
                    {
                        LoadResourceLevel(false, VIDEO_TEST_LEVEL, VIDEO_TEST_MUSIC, VIDEO_TEST_VIDEO);
                        break;
                    }
            }
        }

        const string SAVE_RESOURCE = "demo/level";
        const string AOTC_RESOURCE = "demo_new/level";
        const string NEW_RESOURCE = "new/level";
        const string NODE_RESOURCE = "node/level";
        const string VIDEO_TEST_LEVEL = "video_test/video_test";
        const string VIDEO_TEST_MUSIC = "video_test/video_test_music";
        const string VIDEO_TEST_VIDEO = "video_test/video_test_bg";

        // StoryManager.LoadResourceLevel(0, "demo/level", "demo/level")
        public static void LoadResourceLevel(bool old, string jsonPath, string audioPath, string videoClipPath = null)
        {
            var json = Resources.Load<TextAsset>($"beatmaps/{jsonPath}");
            var audio = Resources.Load<AudioClip>($"beatmaps/{audioPath}");
            var jnPlayers = JSON.Parse("{}");
            for (int i = 0; i < 4; i++)
                jnPlayers["indexes"][i] = PlayerModel.BETA_ID;

            GameData gameData = old ? ParseSave(JSON.Parse(json.text)) : GameData.Parse(JSON.Parse(LevelManager.UpdateBeatmap(json.text, "1.0.0")));

            if (jsonPath == "demo_new/level")
            {
                gameData.beatmapThemes.Add(new BeatmapTheme()
                {
                    id = "003051",
                    name = "PA Ahead of the Curve",
                    guiColor = new Color(0.1294f, 0.1216f, 0.1294f, 1f),
                    guiAccentColor = new Color(0.1294f, 0.1216f, 0.1294f, 1f),
                    backgroundColor = new Color(0.9686f, 0.9529f, 0.9686f, 1f),
                    backgroundColors = new List<Color>()
                    {
                        new Color(0.9686f, 0.9529f, 0.9686f, 1f),
                        new Color(0.1882f, 0.1882f, 0.1882f, 1f),
                        new Color(0.9176f, 0.9176f, 0.9176f, 1f),
                        new Color(0.7176f, 0.7176f, 0.7176f, 1f),
                        new Color(0.3059f, 0.3059f, 0.3059f, 1f),
                        new Color(0.298f, 0.298f, 0.298f, 1f),
                        new Color(0.7608f, 0.0941f, 0.3569f, 1f),
                        new Color(0.6784f, 0.0784f, 0.3412f, 1f),
                        new Color(0.5333f, 0.0549f, 0.3098f, 1f),
                    },
                    objectColors = new List<Color>()
                    {
                        Color.white,
                        new Color(0.0627f, 0.8941f, 0.3373f, 1f),
                        new Color(0.1804f, 0.3569f, 0.9686f, 1f),
                    }.Fill(15, new Color(0.1804f, 0.3569f, 0.9686f, 1f)),
                    playerColors = new List<Color>()
                    {
                        new Color(0.7569f, 0f, 0.0078f, 1f),
                        new Color(0.0471f, 0.0863f, 0.9686f, 1f),
                        new Color(0.1294f, 0.9882f, 0.0118f, 1f),
                        new Color(0.9922f, 0.5922f, 0.0039f, 1f),
                    },
                    effectColors = new List<Color>().Fill(18, Color.white),
                });
                gameData.events[4][0].values[0] = 3051;
            }

            var id = jsonPath switch
            {
                "demo/level" => "1",
                "demo_new/level" => "2",
                "new/level" => "3",
                "node/level" => "4",
                "video_test/video_test" => "5",
                _ => "0"
            };
            var storyLevel = new StoryLevel
            {
                id = id,
                json = gameData.ToJSON().ToString(),
                jsonPlayers = jnPlayers.ToString(),
                metadata = new MetaData
                {
                    arcadeID = id,
                    uploaderName = "Vitamin Games",
                    creator = new CreatorMetaData
                    {
                        name = "Pidge",
                    },
                    beatmap = new BeatmapMetaData
                    {
                        name = jsonPath switch
                        {
                            "demo/level" => "Beluga Bugatti",
                            "demo_new/level" => "Ahead of the Curve",
                            "new/level" => "new",
                            "node/level" => "Node",
                            "video_test/video_test" => "miku",
                            _ => string.Empty
                        }
                    },
                    song = new SongMetaData
                    {
                        title = jsonPath switch
                        {
                            "demo/level" => "Save",
                            "demo_new/level" => "Ahead of the Curve",
                            "new/level" => "Staring Down the Barrels",
                            "node/level" => "Node",
                            "video_test/video_test" => "miku",
                            _ => string.Empty
                        }
                    },
                    artist = new ArtistMetaData
                    {
                        name = jsonPath switch
                        {
                            "demo/level" => "meganeko",
                            "demo_new/level" => "Creo",
                            "new/level" => "Creo",
                            "node/level" => "meganeko",
                            "video_test/video_test" => "miku",
                            _ => string.Empty
                        }
                    },
                },
                music = audio,
                isResourcesBeatmap = true,
            };

            if (!string.IsNullOrEmpty(videoClipPath))
                storyLevel.videoClip = Resources.Load<UnityEngine.Video.VideoClip>($"beatmaps/{videoClipPath}");

            LevelManager.OnLevelStart = level =>
            {
                Example.Current?.brain?.Notice(ExampleBrain.Notices.GAME_FILE_EASTER_EGG);
            };
            LevelManager.Play(storyLevel, () =>
            {
                LevelManager.OnLevelEnd = null;
                SceneHelper.LoadScene(SceneName.Main_Menu);
            });
        }

        public static GameData ParseSave(JSONNode jn)
        {
            var gameData = new GameData();

            gameData.data = new BeatmapData();
            gameData.data.checkpoints = new List<Checkpoint>();
            gameData.data.level = new LevelData();

            gameData.data.markers = gameData.data.markers.OrderBy(x => x.time).ToList();

            CoreHelper.Log($"Parsing checkpoints...");
            for (int i = 0; i < jn["levelData"]["checkpoints"].Count; i++)
            {
                var jnCheckpoint = jn["levelData"]["checkpoints"][i];
                gameData.data.checkpoints.Add(new Checkpoint(jnCheckpoint["name"], jnCheckpoint["time"].AsFloat, jnCheckpoint["pos"].AsVector2()));
            }

            CoreHelper.Log($"Update...");
            gameData.data.checkpoints = gameData.data.checkpoints.OrderBy(x => x.time).ToList();

            CoreHelper.Log($"Parsing themes...");
            if (jn["themes"] != null)
                for (int i = 0; i < jn["themes"].Count; i++)
                {
                    if (string.IsNullOrEmpty(jn["themes"][i]["id"]))
                        continue;

                    gameData.AddTheme(BeatmapTheme.Parse(jn["themes"][i]));
                }

            ThemeManager.inst.UpdateAllThemes();

            CoreHelper.Log($"Parsing beatmap objects...");
            for (int i = 0; i < jn["beatmapObjects"].Count; i++)
            {
                var jnObject = jn["beatmapObjects"][i];
                var beatmapObject = new BeatmapObject();
                if (!string.IsNullOrEmpty(jnObject["id"]))
                    beatmapObject.id = jnObject["id"];
                if (!string.IsNullOrEmpty(jnObject["parent"]))
                    beatmapObject.Parent = jnObject["parent"];
                beatmapObject.Depth = jnObject["layer"].AsInt;
                beatmapObject.objectType = jnObject["helper"].AsBool ? BeatmapObject.ObjectType.Helper : BeatmapObject.ObjectType.Normal;
                beatmapObject.StartTime = jnObject["startTime"].AsFloat;
                beatmapObject.name = jnObject["name"];
                beatmapObject.origin = Parser.TryParse(jnObject["origin"], Vector2.zero);
                beatmapObject.editorData = new ObjectEditorData(jnObject["editorData"]["bin"].AsInt, jnObject["editorData"]["layer"].AsInt, false, false);

                var events = new List<List<EventKeyframe>>();
                events.Add(new List<EventKeyframe>());
                events.Add(new List<EventKeyframe>());
                events.Add(new List<EventKeyframe>());
                events.Add(new List<EventKeyframe>());

                float eventTime = 0f;
                var lastPos = Vector2.zero;
                var lastSca = Vector2.one;
                var lastCol = 0;

                for (int j = 0; j < jnObject["events"].Count; j++)
                {
                    var jnEvent = jnObject["events"][j];
                    eventTime += jnEvent["eventTime"].AsFloat;

                    bool hasPos = false;
                    bool hasSca = false;
                    bool hasRot = false;
                    bool hasCol = false;

                    for (int k = 0; k < jnEvent["eventParts"].Count; k++)
                    {
                        switch (jnEvent["eventParts"][k]["kind"].AsInt)
                        {
                            case 0:
                                {
                                    if (hasPos)
                                        break;

                                    var eventKeyframe = new EventKeyframe(eventTime, new float[] { jnEvent["eventParts"][k]["value0"].AsFloat, jnEvent["eventParts"][k]["value1"].AsFloat, }, new float[] { jnEvent["eventParts"][k]["valueR0"].AsFloat, jnEvent["eventParts"][k]["valueR1"].AsFloat, }, jnEvent["eventParts"][k]["random"].AsBool == true ? 1 : 0);
                                    lastPos = new Vector2(eventKeyframe.values[0], eventKeyframe.values[1]);
                                    events[0].Add(eventKeyframe);
                                    hasPos = true;
                                    break;
                                }
                            case 1:
                                {
                                    if (hasSca)
                                        break;

                                    var eventKeyframe = new EventKeyframe(eventTime, new float[] { jnEvent["eventParts"][k]["value0"].AsFloat, jnEvent["eventParts"][k]["value1"].AsFloat, }, new float[] { jnEvent["eventParts"][k]["valueR0"].AsFloat, jnEvent["eventParts"][k]["valueR1"].AsFloat, }, jnEvent["eventParts"][k]["random"].AsBool == true ? 1 : 0);
                                    lastSca = new Vector2(eventKeyframe.values[0], eventKeyframe.values[1]);
                                    events[1].Add(eventKeyframe);
                                    hasSca = true;
                                    break;
                                }
                            case 2:
                                {
                                    if (hasRot)
                                        break;

                                    var eventKeyframe = new EventKeyframe(eventTime, new float[] { jnEvent["eventParts"][k]["value0"].AsFloat, }, new float[] { jnEvent["eventParts"][k]["valueR0"].AsFloat, }, jnEvent["eventParts"][k]["random"].AsBool == true ? 1 : 0) { relative = true, };
                                    events[2].Add(eventKeyframe);
                                    hasRot = true;
                                    break;
                                }
                            case 3:
                                {
                                    if (hasCol)
                                        break;

                                    var eventKeyframe = new EventKeyframe(eventTime, new float[] { jnEvent["eventParts"][k]["value0"].AsFloat, }, new float[] { });
                                    lastCol = (int)eventKeyframe.values[0];
                                    events[3].Add(eventKeyframe);
                                    hasCol = true;
                                    break;
                                }
                        }
                    }

                    if (!hasPos)
                        events[0].Add(new EventKeyframe(eventTime, new float[] { lastPos.x, lastPos.y }, new float[] { }));
                    if (!hasSca)
                        events[1].Add(new EventKeyframe(eventTime, new float[] { lastSca.x, lastSca.y }, new float[] { }));
                    if (!hasRot)
                        events[2].Add(new EventKeyframe(eventTime, new float[] { 0f }, new float[] { }) { relative = true });
                    if (!hasCol)
                        events[3].Add(new EventKeyframe(eventTime, new float[] { lastCol }, new float[] { }));
                }

                beatmapObject.events = events;

                gameData.beatmapObjects.Add(beatmapObject);
            }

            gameData.assets.Clear();

            CoreHelper.Log($"Parsing background objects...");
            for (int i = 0; i < jn["backgroundObjects"].Count; i++)
            {
                var jnObject = jn["backgroundObjects"][i];
                gameData.backgroundObjects.Add(new BackgroundObject()
                {
                    name = jnObject["name"],
                    pos = Parser.TryParse(jnObject["pos"], Vector2.zero),
                    scale = Parser.TryParse(jnObject["size"], Vector2.zero),
                    rot = jnObject["rot"].AsFloat,
                    color = jnObject["color"].AsInt,
                    depth = jnObject["layer"].AsInt,
                    drawFade = jnObject["fade"].AsBool,
                    reactiveType = jnObject["reactiveSettings"]["active"].AsBool ? BackgroundObject.ReactiveType.Mids : BackgroundObject.ReactiveType.None,
                });
            }

            var allEvents = new List<List<EventKeyframe>>();
            allEvents.Add(new List<EventKeyframe>()); // move
            allEvents.Add(new List<EventKeyframe>()); // zoom
            allEvents.Add(new List<EventKeyframe>()); // rotate
            allEvents.Add(new List<EventKeyframe>()); // shake
            allEvents.Add(new List<EventKeyframe>()); // theme

            CoreHelper.Log($"Parsing event objects...");
            var eventObjects = jn["eventObjects"].Children.OrderBy(x => x["startTime"].AsFloat).ToList();
            var lastMove = Vector2.zero;
            var lastZoom = 0f;
            var lastRotate = 0f;
            //var lastShake = 0f;
            for (int i = 0; i < eventObjects.Count; i++)
            {
                var jnObject = eventObjects[i];
                var startTime = jnObject["startTime"].AsFloat;
                var eventTime = jnObject["eventTime"].AsFloat;

                bool hasMove = false;
                bool hasZoom = false;
                bool hasRotate = false;
                //bool hasShake = false;

                for (int j = 0; j < jnObject["events"].Count; j++)
                {
                    var jnEvent = jnObject["events"][j];

                    switch (jnEvent["kind"].AsInt)
                    {
                        case 0:
                            {
                                var eventKeyframe = new EventKeyframe(startTime, new float[] { lastMove.x, lastMove.y, }, new float[] { });
                                allEvents[0].Add(eventKeyframe);

                                eventKeyframe = new EventKeyframe(startTime + eventTime, new float[] { jnEvent["value0"].AsFloat, jnEvent["value1"].AsFloat, }, new float[] { });
                                lastMove = new Vector2(eventKeyframe.values[0], eventKeyframe.values[1]);
                                allEvents[0].Add(eventKeyframe);
                                hasMove = true;
                                break;
                            }
                        case 1:
                            {
                                var eventKeyframe = new EventKeyframe(startTime, new float[] { lastZoom, }, new float[] { });
                                allEvents[1].Add(eventKeyframe);

                                eventKeyframe = new EventKeyframe(startTime + eventTime, new float[] { jnEvent["value0"].AsFloat, }, new float[] { });
                                lastZoom = eventKeyframe.values[0];
                                allEvents[1].Add(eventKeyframe);
                                hasZoom = true;
                                break;
                            }
                        case 2:
                            {
                                var eventKeyframe = new EventKeyframe(startTime, new float[] { lastRotate, }, new float[] { });
                                allEvents[2].Add(eventKeyframe);

                                eventKeyframe = new EventKeyframe(startTime + eventTime, new float[] { jnEvent["value0"].AsFloat, }, new float[] { });
                                lastRotate = eventKeyframe.values[0];
                                allEvents[2].Add(eventKeyframe);
                                hasRotate = true;
                                break;
                            }
                            //case 3:
                            //    {
                            //        var eventKeyframe = new EventKeyframe(startTime, new float[] { lastShake, }, new float[] { });
                            //        allEvents[3].Add(eventKeyframe);

                            //        eventKeyframe = new EventKeyframe(startTime + eventTime, new float[] { jnEvent["value0"].AsFloat * 0.1f, }, new float[] { });
                            //        lastShake = eventKeyframe.eventValues[0];
                            //        allEvents[3].Add(eventKeyframe);
                            //        hasShake = true;
                            //        break;
                            //    }
                    }

                    if (!hasMove)
                    {
                        allEvents[0].Add(new EventKeyframe(startTime, new float[] { lastMove.x, lastMove.y }, new float[] { }));
                        allEvents[0].Add(new EventKeyframe(startTime + eventTime, new float[] { lastMove.x, lastMove.y }, new float[] { }));
                    }
                    if (!hasZoom)
                    {
                        allEvents[1].Add(new EventKeyframe(startTime, new float[] { lastZoom }, new float[] { }));
                        allEvents[1].Add(new EventKeyframe(startTime + eventTime, new float[] { lastZoom }, new float[] { }));
                    }
                    if (!hasRotate)
                    {
                        allEvents[2].Add(new EventKeyframe(startTime, new float[] { lastRotate }, new float[] { }));
                        allEvents[2].Add(new EventKeyframe(startTime + eventTime, new float[] { lastRotate }, new float[] { }));
                    }
                    //if (!hasShake)
                    //{
                    //    allEvents[3].Add(new EventKeyframe(startTime, new float[] { lastShake }, new float[] { }));
                    //    allEvents[3].Add(new EventKeyframe(startTime + eventTime, new float[] { lastShake }, new float[] { }));
                    //}
                }
            }

            allEvents[4].Add(new EventKeyframe(0f, new float[] { 1f }, new float[] { }));

            gameData.events = allEvents;

            GameData.ClampEventListValues(gameData.events);

            return gameData;
        }

        #endregion

        /// <summary>
        /// Plays a story level directly from a path.
        /// </summary>
        /// <param name="path">Path to a story level.</param>
        public void Play(string path, string songName = null) => StartCoroutine(IPlay(path, songName));

        /// <summary>
        /// Gets a story level from the <see cref="StoryMode"/> and plays it.
        /// </summary>
        /// <param name="chapter">Chapter index of the story level.</param>
        /// <param name="level">Index of the story level.</param>
        /// <param name="bonus">If the story level is from a bonus chapter.</param>
        /// <param name="skipCutscenes">If cutscenes should be skipped.</param>
        public void Play(int chapter, int level, int cutsceneIndex = 0, bool bonus = false, bool skipCutscenes = false)
        {
            var storyChapter = (bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters)[chapter];
            var storyLevel = storyChapter.GetLevel(level);

            currentPlayingChapterIndex = chapter;
            currentPlayingLevelSequenceIndex = level;

            StartCoroutine(IPlay(storyLevel, cutsceneIndex, skipCutscenes));
        }

        public void PlayCutscene(int chapter, int level, CutsceneDestination cutsceneDestination, int cutsceneIndex, bool bonus = false, Action onLevelEnd = null)
        {
            var storyChapter = (bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters)[chapter];
            var storyLevel = storyChapter.GetLevel(level);

            currentPlayingChapterIndex = chapter;
            currentPlayingLevelSequenceIndex = level;

            StartCoroutine(IPlayCutscene(storyLevel, cutsceneDestination, cutsceneIndex, onLevelEnd));
        }

        public void PlayAllCutscenes(int chapter, bool bonus = false)
        {
            var level = 0;
            var cutsceneDestination = CutsceneDestination.Pre;
            var cutsceneIndex = 0;
            PlayCutsceneRecursive(chapter, level, cutsceneDestination, cutsceneIndex, bonus);
        }

        void PlayCutsceneRecursive(int chapter, int level, CutsceneDestination cutsceneDestination, int cutsceneIndex, bool bonus)
        {
            var storyChapter = (bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters)[chapter];

            PlayCutscene(chapter, level, cutsceneDestination, cutsceneIndex, bonus, () =>
            {
                var storyLevel = storyChapter.GetLevel(level);
                cutsceneIndex++;

                // find next cutscene
                while (true)
                {
                    // end of pre-cutscenes
                    if (cutsceneDestination == CutsceneDestination.Pre && cutsceneIndex >= storyLevel.preCutscenes.Count)
                    {
                        cutsceneDestination = CutsceneDestination.Post;
                        cutsceneIndex = 0;
                    }

                    // end of post-cutscenes
                    if (cutsceneDestination == CutsceneDestination.Post && cutsceneIndex >= storyLevel.postCutscenes.Count)
                    {
                        level++;
                        if (level >= storyChapter.Count + 1)
                        {
                            SceneHelper.LoadInterfaceScene();
                            return;
                        }

                        storyLevel = storyChapter.GetLevel(level);
                        cutsceneDestination = CutsceneDestination.Pre;
                        cutsceneIndex = 0;
                        continue;
                    }

                    // prevents playing cutscenes of levels that aren't complete. (e.g secret)
                    if (!CurrentSave.LoadBool($"DOC{RTString.ToStoryNumber(chapter)}_{RTString.ToStoryNumber(level)}Complete", false))
                    {
                        level++;
                        if (level >= storyChapter.Count + 1)
                        {
                            SceneHelper.LoadInterfaceScene();
                            return;
                        }

                        storyLevel = storyChapter.GetLevel(level);
                        cutsceneDestination = CutsceneDestination.Pre;
                        cutsceneIndex = 0;
                        continue;
                    }

                    break;
                }

                PlayCutsceneRecursive(chapter, level, cutsceneDestination, cutsceneIndex, bonus);
            });
        }

        /// <summary>
        /// Plays a story level.
        /// </summary>
        /// <param name="level">The current level sequence.</param>
        /// <param name="cutsceneIndex">The current index of the level sequence.</param>
        /// <param name="skipCutscenes">If cutscenes should be skipped.</param>
        public IEnumerator IPlay(StoryMode.LevelSequence level, int cutsceneIndex = 0, bool skipCutscenes = false)
        {
            var path = level.filePath;
            bool isCutscene = false;

            int chapterIndex = CurrentSave.ChapterIndex;
            int levelIndex = CurrentSave.LoadInt($"DOC{RTString.ToStoryNumber(chapterIndex)}Progress", 0);

            if (!skipCutscenes && cutsceneIndex >= 0 && cutsceneIndex < level.Count && level.Count > 1 && cutsceneIndex != level.preCutscenes.Count)
            {
                isCutscene = true;
                path = level[cutsceneIndex];
            }

            if (!RTFile.FileExists(path))
            {
                CoreHelper.LogError($"File \'{path}\' does not exist.");
                SoundManager.inst.PlaySound(DefaultSounds.Block);
                Loaded = false;
                CoreHelper.InStory = false;
                LevelManager.OnLevelEnd = null;
                SceneHelper.LoadScene(SceneName.Main_Menu);
                yield break;
            }

            CoreHelper.Log($"Loading story mode level... {path}");
            if (RTFile.FileIsFormat(path, FileFormat.LSB))
            {
                var storyLevel = new Level(RTFile.GetDirectory(path)) { isStory = true };
                AssignStoryLevelMusic(path.songName, storyLevel);
                LevelManager.Play(storyLevel, () => OnLevelEnd(level, isCutscene, cutsceneIndex));
                yield break;
            }

            StartCoroutine(StoryLevel.LoadFromAsset(path, storyLevel =>
            {
                Loaded = true;

                CoreHelper.InStory = true;

                if (storyLevel == null)
                {
                    LevelManager.OnLevelEnd = null;
                    SceneHelper.LoadInterfaceScene();
                    return;
                }

                AssignStoryLevelMusic(path.songName, storyLevel);

                if (!storyLevel.music)
                {
                    CoreHelper.LogError($"Music is null for some reason wtf");
                    return;
                }

                LevelManager.Play(storyLevel, () => OnLevelEnd(level, isCutscene, cutsceneIndex));
            }));

            yield break;
        }

        public IEnumerator IPlayCutscene(StoryMode.LevelSequence level, CutsceneDestination cutsceneDestination, int cutsceneIndex, Action onLevelEnd = null)
        {
            var path = level.GetPaths(cutsceneDestination)[cutsceneIndex];

            int chapterIndex = CurrentSave.ChapterIndex;
            int levelIndex = CurrentSave.LoadInt($"DOC{RTString.ToStoryNumber(chapterIndex)}Progress", 0);

            if (!RTFile.FileExists(path))
            {
                CoreHelper.LogError($"File \'{path}\' does not exist.");
                SoundManager.inst.PlaySound(DefaultSounds.Block);
                Loaded = false;
                CoreHelper.InStory = false;
                LevelManager.OnLevelEnd = null;
                SceneHelper.LoadScene(SceneName.Main_Menu);
                yield break;
            }

            CoreHelper.Log($"Loading story mode level... {path}");
            if (RTFile.FileIsFormat(path, FileFormat.LSB))
            {
                var storyLevel = new Level(RTFile.GetDirectory(path)) { isStory = true };
                AssignStoryLevelMusic(path.songName, storyLevel);
                LevelManager.Play(storyLevel, () =>
                {
                    LevelManager.OnLevelEnd = null;
                    if (onLevelEnd != null)
                    {
                        onLevelEnd();
                        return;
                    }

                    SceneHelper.LoadInterfaceScene();
                });
                yield break;
            }

            StartCoroutine(StoryLevel.LoadFromAsset(path, storyLevel =>
            {
                Loaded = true;

                CoreHelper.InStory = true;

                if (storyLevel == null)
                {
                    LevelManager.OnLevelEnd = null;
                    if (onLevelEnd != null)
                    {
                        onLevelEnd();
                        return;
                    }

                    SceneHelper.LoadInterfaceScene();
                    return;
                }

                AssignStoryLevelMusic(path.songName, storyLevel);

                if (!storyLevel.music)
                {
                    CoreHelper.LogError($"Music is null for some reason wtf");
                    return;
                }

                LevelManager.Play(storyLevel, () =>
                {
                    LevelManager.OnLevelEnd = null;
                    if (onLevelEnd != null)
                    {
                        onLevelEnd();
                        return;
                    }

                    SceneHelper.LoadInterfaceScene();
                });
            }));

            yield break;
        }

        /// <summary>
        /// Plays a story level directly from a path.
        /// </summary>
        /// <param name="path">Path to a story level.</param>
        public IEnumerator IPlay(string path, string songName = null)
        {
            if (!RTFile.FileExists(path))
            {
                CoreHelper.LogError($"File \'{path}\' does not exist.");
                SoundManager.inst.PlaySound(DefaultSounds.Block);
                Loaded = false;
                CoreHelper.InStory = false;
                LevelManager.OnLevelEnd = null;
                SceneHelper.LoadScene(SceneName.Main_Menu);
                yield break;
            }

            CoreHelper.Log($"Loading story mode level... {path}");
            if (RTFile.FileIsFormat(path, FileFormat.LSB))
            {
                var storyLevel = new Level(RTFile.GetDirectory(path)) { isStory = true };
                AssignStoryLevelMusic(songName, storyLevel);
                LevelManager.Play(storyLevel, () =>
                {
                    LevelManager.OnLevelEnd = null;
                    SceneHelper.LoadInterfaceScene();
                });
                yield break;
            }

            StartCoroutine(StoryLevel.LoadFromAsset(path, storyLevel =>
            {
                Loaded = true;

                CoreHelper.InStory = true;

                if (storyLevel == null)
                {
                    LevelManager.OnLevelEnd = null;
                    SceneHelper.LoadInterfaceScene();
                    return;
                }

                AssignStoryLevelMusic(songName, storyLevel);

                if (!storyLevel.music)
                {
                    CoreHelper.LogError($"Music is null for some reason wtf");
                    return;
                }

                LevelManager.Play(storyLevel, () =>
                {
                    LevelManager.OnLevelEnd = null;
                    SceneHelper.LoadInterfaceScene();
                });
            }));

            yield break;
        }

        void UnlockChapterAchievement(int chapter) => AchievementManager.inst.UnlockAchievement($"story_doc{RTString.ToStoryNumber(chapter)}_complete");

        void OnLevelEnd()
        {
            LevelManager.Clear();
            RTLevel.Current?.Clear();
            CurrentSave.UpdateCurrentLevelProgress(); // allow players to get a better rank

            if (!ContinueStory)
            {
                Return();
                return;
            }

            int chapter = CurrentSave.ChapterIndex;
            int level = CurrentSave.LoadInt($"DOC{RTString.ToStoryNumber(chapter)}Progress", 0);

            if (chapter >= StoryMode.Instance.chapters.Count)
            {
                Return();
                return;
            }

            level++;
            if (level >= StoryMode.Instance.chapters[chapter].levels.Count)
            {
                UnlockChapterAchievement(chapter);
                chapter++;
                level = 0;
            }

            CurrentSave.SaveProgress(chapter, level);

            CoreHelper.InStory = true;
            LevelManager.OnLevelEnd = null;
            SceneHelper.LoadInterfaceScene();
        }

        void OnLevelEnd(StoryMode.LevelSequence level, bool isCutscene = false, int cutsceneIndex = 0)
        {
            LevelManager.Clear();
            RTLevel.Reinit(false);
            if (!isCutscene)
                CurrentSave.UpdateCurrentLevelProgress(); // allow players to get a better rank

            int chapterIndex = currentPlayingChapterIndex;
            int levelIndex = currentPlayingLevelSequenceIndex;

            if (!isCutscene && !level.isChapterTransition)
                CurrentSave.SaveBool($"DOC{RTString.ToStoryNumber(chapterIndex)}_{RTString.ToStoryNumber(levelIndex)}Complete", true);

            if (!ContinueStory)
            {
                Return();
                return;
            }

            cutsceneIndex++;
            levelIndex++;

            CurrentSave.SaveProgress(chapterIndex, levelIndex);

            // chapter completion should only occur after beating the transition level.
            if (!isCutscene && level.isChapterTransition)
            {
                UnlockChapterAchievement(chapterIndex);
                CurrentSave.SaveBool($"DOC{RTString.ToStoryNumber(chapterIndex)}Complete", true);
                chapterIndex++;
                levelIndex = 0;

                CurrentSave.SaveProgress(chapterIndex, levelIndex);
            }

            if (cutsceneIndex < level.Count)
            {
                StartCoroutine(IPlay(level, cutsceneIndex));
                return;
            }

            if (chapterIndex >= StoryMode.Instance.chapters.Count)
            {
                StoryCompletion();
                return;
            }

            CoreHelper.InStory = true;
            LevelManager.OnLevelEnd = null;
            InterfaceManager.inst.onReturnToStoryInterface = () => InterfaceManager.inst.ParseInterface(level.returnInterface);
            SceneHelper.LoadInterfaceScene();
        }

        void Return()
        {
            CoreHelper.InStory = true;
            LevelManager.OnLevelEnd = null;
            ContinueStory = true;
            SceneHelper.LoadInterfaceScene();
        }

        // function to run when completing the story mode
        void StoryCompletion()
        {
            SoundManager.inst.PlaySound(DefaultSounds.loadsound);
            CoreHelper.InStory = false;
            LevelManager.OnLevelEnd = null;
            SceneHelper.LoadScene(SceneName.Main_Menu);
        }

        bool loadCredits = false;

        void AssignStoryLevelMusic(string songName, Level level)
        {
            if (string.IsNullOrEmpty(songName) || !SoundManager.inst.TryGetMusic(songName, out AudioClip audioClip))
                return;

            CoreHelper.Log($"Setting song to: {songName}");
            level.music = audioClip;
        }

        #endregion
    }
}