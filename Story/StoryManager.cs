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
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Menus;

namespace BetterLegacy.Story
{
    /// <summary>
    /// Manager class for handling the BetterLegacy (Classic Arrhythmia) story mode.
    /// </summary>
    public class StoryManager : BaseManager<StoryManager, ManagerSettings>
    {
        #region Values

        /// <summary>
        /// Path to the story assets folder.
        /// </summary>
        public static string StoryAssetsPath => AssetPack.GetDirectory("story");

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

        public StoryFunctions functions = new StoryFunctions();

        #region Progression

        /// <summary>
        /// The default chapter rank requirement for "bonuses" to be unlocked. In this case, the player needs to get higher than a B rank (S / SS / A rank).
        /// </summary>
        public const int CHAPTER_RANK_REQUIREMENT = 3;

        public StorySelection currentStorySelection;

        public bool isCutscene;

        public int currentPlayingChapterIndex;
        public int currentPlayingLevelSequenceIndex;
        public bool inBonusChapter;

        /// <summary>
        /// The story mode chapter that is currently open.
        /// </summary>
        public StoryMode.Chapter CurrentChapter => (inBonusChapter ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters)[currentPlayingChapterIndex];
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

        #endregion

        #region Functions

        public override void OnInit() => StoryMode.Init();

        #region Resource

        public override void OnTick()
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
            new SecretSequence(new List<KeyCode> { KeyCode.N, KeyCode.O, KeyCode.D, KeyCode.E, }, () =>
            {
                SoundManager.inst.PlaySound(inst.gameObject, DefaultSounds.loadsound);

                if (Editor.Managers.RTEditor.inst)
                {
                    Editor.Managers.RTEditor.inst.ShowWarningPopup("Are you sure you want to continue?", () =>
                    {
                        AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                        LoadResourceLevel(true, NODE_RESOURCE, NODE_RESOURCE);
                    }, Editor.Managers.RTEditor.inst.HideWarningPopup);
                    return;
                }

                AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                LoadResourceLevel(true, NODE_RESOURCE, NODE_RESOURCE);
            }), // load old node level (not the boss level unfortunately ;-;)
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
            }), // load miku video test
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

            if (jsonPath == AOTC_RESOURCE)
                gameData.events[4][0].values[0] = 799099;

            var id = jsonPath switch
            {
                SAVE_RESOURCE => "1",
                AOTC_RESOURCE => "2",
                NEW_RESOURCE => "3",
                NODE_RESOURCE => "4",
                VIDEO_TEST_LEVEL => "5",
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
                            SAVE_RESOURCE => "Beluga Bugatti",
                            AOTC_RESOURCE => "Ahead of the Curve",
                            NEW_RESOURCE => "new",
                            NODE_RESOURCE => "Node",
                            VIDEO_TEST_LEVEL => "miku",
                            _ => string.Empty
                        }
                    },
                    song = new SongMetaData
                    {
                        title = jsonPath switch
                        {
                            SAVE_RESOURCE => "Save",
                            AOTC_RESOURCE => "Ahead of the Curve",
                            NEW_RESOURCE => "Staring Down the Barrels",
                            NODE_RESOURCE => "Node",
                            VIDEO_TEST_LEVEL => "miku",
                            _ => string.Empty
                        }
                    },
                    artist = new ArtistMetaData
                    {
                        name = jsonPath switch
                        {
                            SAVE_RESOURCE => "meganeko",
                            AOTC_RESOURCE => "Creo",
                            NEW_RESOURCE => "Creo",
                            NODE_RESOURCE => "meganeko",
                            VIDEO_TEST_LEVEL => "miku",
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
                            case 0: {
                                    if (hasPos)
                                        break;

                                    var eventKeyframe = new EventKeyframe(eventTime, new float[] { jnEvent["eventParts"][k]["value0"].AsFloat, jnEvent["eventParts"][k]["value1"].AsFloat, }, new float[] { jnEvent["eventParts"][k]["valueR0"].AsFloat, jnEvent["eventParts"][k]["valueR1"].AsFloat, }, jnEvent["eventParts"][k]["random"].AsBool == true ? 1 : 0);
                                    lastPos = new Vector2(eventKeyframe.values[0], eventKeyframe.values[1]);
                                    events[0].Add(eventKeyframe);
                                    hasPos = true;
                                    break;
                                }
                            case 1: {
                                    if (hasSca)
                                        break;

                                    var eventKeyframe = new EventKeyframe(eventTime, new float[] { jnEvent["eventParts"][k]["value0"].AsFloat, jnEvent["eventParts"][k]["value1"].AsFloat, }, new float[] { jnEvent["eventParts"][k]["valueR0"].AsFloat, jnEvent["eventParts"][k]["valueR1"].AsFloat, }, jnEvent["eventParts"][k]["random"].AsBool == true ? 1 : 0);
                                    lastSca = new Vector2(eventKeyframe.values[0], eventKeyframe.values[1]);
                                    events[1].Add(eventKeyframe);
                                    hasSca = true;
                                    break;
                                }
                            case 2: {
                                    if (hasRot)
                                        break;

                                    var eventKeyframe = new EventKeyframe(eventTime, new float[] { jnEvent["eventParts"][k]["value0"].AsFloat, }, new float[] { jnEvent["eventParts"][k]["valueR0"].AsFloat, }, jnEvent["eventParts"][k]["random"].AsBool == true ? 1 : 0) { relative = true, };
                                    events[2].Add(eventKeyframe);
                                    hasRot = true;
                                    break;
                                }
                            case 3: {
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
                        events[0].Add(new EventKeyframe(eventTime, new float[3], new float[3]) { relative = true });
                    if (!hasSca)
                        events[1].Add(new EventKeyframe(eventTime, new float[2], new float[3]) { relative = true });
                    if (!hasRot)
                        events[2].Add(new EventKeyframe(eventTime, new float[1], new float[3]) { relative = true });
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
        /// Gets a story level from the <see cref="StoryMode"/> and plays it.
        /// </summary>
        /// <param name="storySelection">Location of the story level.</param>
        public void Play(StorySelection storySelection, Action onLevelEnd = null)
        {
            // get story chapter and story level.
            var storyChapter = (storySelection.bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters)[storySelection.chapter];
            var storyLevel = storyChapter.GetLevel(storySelection.level);

            inBonusChapter = storySelection.bonus;
            currentStorySelection = storySelection;
            currentPlayingChapterIndex = storySelection.chapter;
            currentPlayingLevelSequenceIndex = storySelection.level;

            var path = storyLevel.filePath;

            // check if the current level part is a cutscene.
            if (!storySelection.skipCutscenes && storySelection.cutsceneIndex >= 0 && storySelection.cutsceneIndex < storyLevel.Count && storyLevel.Count > 1 && storySelection.cutsceneIndex != storyLevel.preCutscenes.Count)
            {
                isCutscene = true;
                path = storyLevel[storySelection.cutsceneIndex];
            }
            else
                isCutscene = false;

            // allow LSB file format.
            if (RTFile.FileIsFormat(path, FileFormat.LSB))
            {
                Loaded = true;
                var level = new Level(RTFile.GetDirectory(path)) { isStory = true };

                // assign story level music. if a song is specified then the level will use that song instead of the one in the files.
                AssignStoryLevelMusic(path.songName, level);
                LevelManager.Play(level, () => EndFunction(storyChapter, storyLevel, path, onLevelEnd));
                return;
            }

            Loaded = false;
            StartCoroutine(StoryLevel.LoadFromAsset(path, level =>
            {
                Loaded = true;
                // assign story level music. if a song is specified then the level will use that song instead of the one in the files.
                AssignStoryLevelMusic(path.songName, level);
                LevelManager.Play(level, () => EndFunction(storyChapter, storyLevel, path, onLevelEnd));
            }));
        }

        /// <summary>
        /// Plays a story level directly from a path.
        /// </summary>
        /// <param name="path">Path to a story level.</param>
        public void Play(string path, string songName = null)
        {
            // allow LSB file format.
            if (RTFile.FileIsFormat(path, FileFormat.LSB))
            {
                Loaded = true;
                var level = new Level(RTFile.GetDirectory(path)) { isStory = true };

                // assign story level music. if a song is specified then the level will use that song instead of the one in the files.
                AssignStoryLevelMusic(songName, level);
                LevelManager.Play(level, () =>
                {
                    LevelManager.Clear();
                    RTLevel.Reinit(false);
                    LevelManager.OnLevelEnd = null;

                    ProjectArrhythmia.State.InStory = true;
                    SceneHelper.LoadInterfaceScene();
                });
                return;
            }

            StartCoroutine(StoryLevel.LoadFromAsset(path, level =>
            {
                Loaded = true;
                // assign story level music. if a song is specified then the level will use that song instead of the one in the files.
                AssignStoryLevelMusic(songName, level);
                LevelManager.Play(level, () =>
                {
                    LevelManager.Clear();
                    RTLevel.Reinit(false);
                    LevelManager.OnLevelEnd = null;

                    ProjectArrhythmia.State.InStory = true;
                    SceneHelper.LoadInterfaceScene();
                });
            }));
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
            Play(new StorySelection
            {
                chapter = chapter,
                level = level,
                cutsceneDestination = cutsceneDestination,
                cutsceneIndex = cutsceneIndex,
                bonus = bonus,
            },
            () =>
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

        void EndFunction(StoryMode.Chapter storyChapter, StoryMode.LevelSequence storyLevel, StoryMode.LevelPath levelPath, Action onLevelEnd = null)
        {
            LevelManager.Clear();
            RTLevel.Reinit(false);
            LevelManager.OnLevelEnd = null;
            onLevelEnd?.Invoke();
            // if the current level part is complete, run the level part end function.
            if (levelPath.onCompleteFunc != null)
                functions.ParseFunction(levelPath.onCompleteFunc, StoryMode.Instance);
            // if the level is complete, run the level end function.
            if (functions.onLevelCompleteFunc != null)
                functions.ParseFunction(functions.onLevelCompleteFunc, StoryMode.Instance);
            if (storyLevel.onCompleteFunc != null && (functions.onLevelCompleteFunc == null || !functions.overrideOnCompleteFunc))
                functions.ParseFunction(storyLevel.onCompleteFunc, StoryMode.Instance);
            // if the level is the final level in a chapter, run the chapter end function.
            if (storyChapter.onCompleteFunc != null && currentPlayingLevelSequenceIndex + 1 == storyChapter.Count)
                functions.ParseFunction(storyChapter.onCompleteFunc, StoryMode.Instance);
        }

        void AssignStoryLevelMusic(string songName, Level level)
        {
            if (string.IsNullOrEmpty(songName) || !SoundManager.inst.TryGetMusic(songName, out AudioClip audioClip))
                return;

            CoreHelper.Log($"Setting song to: {songName}");
            level.music = audioClip;
        }

        #endregion

        public class StoryFunctions : JSONFunctionParser<StoryMode>
        {
            public JSONNode onLevelCompleteFunc;

            public bool overrideOnCompleteFunc;

            public override bool IfFunction(JSONNode jn, string name, JSONNode parameters, StoryMode thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                switch (name)
                {
                    case "IsCutscene": return inst.isCutscene;
                }

                return base.IfFunction(jn, name, parameters, thisElement, customVariables);
            }

            public override void Function(JSONNode jn, string name, JSONNode parameters, StoryMode thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                switch (name)
                {
                    case "SetOnLevelCompleteFunc": {
                            if (parameters == null)
                                return;

                            onLevelCompleteFunc = parameters.Get(0, "func");
                            overrideOnCompleteFunc = parameters.Get(1, "override").AsBool;

                            return;
                        }
                    case "ReturnToStoryInterface": {
                            if (!inst || !inst.CurrentLevelSequence)
                                return;

                            ProjectArrhythmia.State.InStory = true;
                            LevelManager.OnLevelEnd = null;
                            InterfaceManager.inst.onReturnToStoryInterface = () => InterfaceManager.inst.ParseInterface(inst.CurrentLevelSequence.returnInterface);
                            SceneHelper.LoadInterfaceScene();

                            return;
                        }
                    case "StoryCompletion": {
                            SoundManager.inst.PlaySound(DefaultSounds.loadsound);
                            ProjectArrhythmia.State.InStory = false;
                            LevelManager.OnLevelEnd = null;
                            SceneHelper.LoadScene(SceneName.Main_Menu);

                            return;
                        }
                }

                base.Function(jn, name, parameters, thisElement, customVariables);
            }

            public override JSONNode VarFunction(JSONNode jn, string name, JSONNode parameters, StoryMode thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                //switch (name)
                //{

                //}

                return base.VarFunction(jn, name, parameters, thisElement, customVariables);
            }
        }
    }
}