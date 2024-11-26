using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Networking;
using System.IO.Compression;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Managers;
using SimpleJSON;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Data.Player;
using UnityEngine.Video;
using System;
using BetterLegacy.Menus;

namespace BetterLegacy.Story
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager inst;

        public static string StoryAssetsPath => $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Story/";

        public bool Loaded { get; set; }

        public bool ContinueStory { get; set; } = true;

        public static StoryMode StoryModeDebugRef => StoryMode.Instance;

        /// <summary>
        /// The default chapter rank requirement for "bonuses" to be unlocked. In this case, the player needs to get higher than an A rank (S / SS rank).
        /// </summary>
        public const int CHAPTER_RANK_REQUIREMENT = 3;

        //public List<List<string>> levelIDs = new List<List<string>>
        //{
        //    new List<string>
        //    {
        //        "0603661088835365", // Granite
        //        "7376679616786413", // Ahead of the Curve
        //        "9784345755418661", // Super Gamer Girl 3D
        //        "9454675971710439", // Slime Boy Color
        //        "6698982684586290", // Node (Para)
        //        "4462948827770399", // ???
        //    },
        //    new List<string>
        //    {
        //        "3434489214197233", // RPM
        //    },
        //};

        /// <summary>
        /// Inits StoryManager.
        /// </summary>
        public static void Init() => new GameObject(nameof(StoryManager), typeof(StoryManager)).transform.SetParent(SystemManager.inst.transform);

        void Awake()
        {
            inst = this;
            Load();
        }

        List<SecretSequence> secretSequences = new List<SecretSequence>()
        {
            new SecretSequence(new Dictionary<int, KeyCode>
            {
                { 0, KeyCode.B },
                { 1, KeyCode.E },
                { 2, KeyCode.L },
                { 3, KeyCode.U },
                { 4, KeyCode.G },
                { 5, KeyCode.A },
            }, () =>
            {
                SoundManager.inst.PlaySound(inst.gameObject, DefaultSounds.loadsound);

                if (Editor.Managers.RTEditor.inst)
                {
                    Editor.Managers.RTEditor.inst.ShowWarningPopup("Are you sure you want to continue?", () =>
                    {
                        AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                        CoreHelper.LoadResourceLevel(0);
                    }, Editor.Managers.RTEditor.inst.HideWarningPopup);
                    return;
                }

                AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                CoreHelper.LoadResourceLevel(0);
            }), // load save
            new SecretSequence(new Dictionary<int, KeyCode>
            {
                { 0, KeyCode.D },
                { 1, KeyCode.E },
                { 2, KeyCode.M },
                { 3, KeyCode.O },
            }, () =>
            {
                SoundManager.inst.PlaySound(inst.gameObject, DefaultSounds.loadsound);

                if (Editor.Managers.RTEditor.inst)
                {
                    Editor.Managers.RTEditor.inst.ShowWarningPopup("Are you sure you want to continue?", () =>
                    {
                        AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                        CoreHelper.LoadResourceLevel(1);
                    }, Editor.Managers.RTEditor.inst.HideWarningPopup);
                    return;
                }

                AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                CoreHelper.LoadResourceLevel(1);
            }), // load old demo
            new SecretSequence(new Dictionary<int, KeyCode>
            {
                { 0, KeyCode.M },
                { 1, KeyCode.I },
                { 2, KeyCode.K },
                { 3, KeyCode.U },
            }, () =>
            {
                SoundManager.inst.PlaySound(inst.gameObject, DefaultSounds.loadsound);

                if (Editor.Managers.RTEditor.inst)
                {
                    Editor.Managers.RTEditor.inst.ShowWarningPopup("Are you sure you want to continue?", () =>
                    {
                        AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                        CoreHelper.LoadResourceLevel(4);
                    }, Editor.Managers.RTEditor.inst.HideWarningPopup);
                    return;
                }

                AchievementManager.inst.UnlockAchievement("discover_hidden_levels");
                CoreHelper.LoadResourceLevel(4);
            }), // load old demo
        };

        public class SecretSequence
        {
            public SecretSequence(Dictionary<int, KeyCode> keys, Action onSequenceEnd)
            {
                this.keys = keys;
                this.onSequenceEnd = onSequenceEnd;
            }

            public int counter;
            public Dictionary<int, KeyCode> keys;
            public Action onSequenceEnd;
        }

        void Update()
        {
            if (CoreHelper.IsUsingInputField)
                return;

            var key = CoreHelper.GetKeyCodeDown();

            if (key == KeyCode.None)
                return;

            for (int i = 0; i < secretSequences.Count; i++)
            {
                var sequence = secretSequences[i];

                sequence.keys.TryGetValue(sequence.counter, out KeyCode keyCompare);

                if (key == keyCompare)
                    sequence.counter++;
                else
                    sequence.counter = 0;

                if (sequence.counter == sequence.keys.Count)
                {
                    sequence.onSequenceEnd?.Invoke();
                    sequence.counter = 0;
                }
            }
        }

        #region Save File

        public int currentPlayingChapterIndex;
        public int currentPlayingLevelSequenceIndex;
        public StoryMode.Chapter CurrentChapter => StoryMode.Instance.chapters[currentPlayingChapterIndex];
        public StoryMode.LevelSequence CurrentLevelSequence => CurrentChapter[currentPlayingLevelSequenceIndex];

        public int ChapterIndex => LoadInt("Chapter", 0);
        public int LevelSequenceIndex => LoadInt($"DOC{(ChapterIndex + 1).ToString("00")}Progress", 0);

        public string StorySavesPath => $"{RTFile.ApplicationDirectory}profile/story_saves_{(SaveSlot + 1).ToString("00")}.lss";
        public JSONNode storySavesJSON;
        int saveSlot;
        public int SaveSlot
        {
            get => saveSlot;
            set
            {
                saveSlot = value;
                Load();
            }
        }

        public List<LevelManager.PlayerData> Saves { get; set; } = new List<LevelManager.PlayerData>();

        public void Load()
        {
            StoryMode.Init();
            storySavesJSON = JSON.Parse(RTFile.FileExists(StorySavesPath) ? RTFile.ReadFromFile(StorySavesPath) : "{}");

            Saves.Clear();
            if (storySavesJSON["lvl"] != null)
                for (int i = 0; i < storySavesJSON["lvl"].Count; i++)
                    Saves.Add(LevelManager.PlayerData.Parse(storySavesJSON["lvl"][i]));
        }

        public void UpdateCurrentLevelProgress()
        {
            if (LevelManager.CurrentLevel == null)
                return;

            var level = LevelManager.CurrentLevel;

            CoreHelper.Log($"Setting Player Data");

            // TODO: Implement achievement system (not the Steam one, the custom one)
            //if (Saves.Where(x => x.Completed).Count() >= 100)
            //{
            //    SteamWrapper.inst.achievements.SetAchievement("GREAT_TESTER");
            //}

            //if (PlayerManager.IsZenMode || PlayerManager.IsPractice)
            //    return;

            var makeNewPlayerData = level.playerData == null;
            if (makeNewPlayerData)
                level.playerData = new LevelManager.PlayerData { ID = level.id, LevelName = level.metadata?.beatmap?.name, };

            CoreHelper.Log($"Updating save data\n" +
                $"New Player Data = {makeNewPlayerData}\n" +
                $"Deaths [OLD = {level.playerData.Deaths} > NEW = {GameManager.inst.deaths.Count}]\n" +
                $"Hits: [OLD = {level.playerData.Hits} > NEW = {GameManager.inst.hits.Count}]\n" +
                $"Boosts: [OLD = {level.playerData.Boosts} > NEW = {LevelManager.BoostCount}]");

            level.playerData.Update(GameManager.inst.deaths.Count, GameManager.inst.hits.Count, LevelManager.BoostCount, true);

            if (Saves.TryFindIndex(x => x.ID == level.id, out int saveIndex))
                Saves[saveIndex] = level.playerData;
            else
                Saves.Add(level.playerData);

            SaveProgress();
        }

        public void SaveProgress()
        {
            storySavesJSON["lvl"] = new JSONArray();
            for (int i = 0; i < Saves.Count; i++)
            {
                storySavesJSON["lvl"][i] = Saves[i].ToJSON();
            }

            Save();
        }

        public void Save()
        {
            try
            {
                RTFile.WriteToFile(StorySavesPath, storySavesJSON.ToString());
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        public void SaveBool(string name, bool value)
        {
            CoreHelper.Log($"Saving {name} > {value}");
            storySavesJSON["saves"][name]["bool"] = value;
            Save();
        }
        
        public void SaveInt(string name, int value)
        {
            CoreHelper.Log($"Saving {name} > {value}");
            storySavesJSON["saves"][name]["int"] = value;
            Save();
        }
        
        public void SaveFloat(string name, float value)
        {
            CoreHelper.Log($"Saving {name} > {value}");
            storySavesJSON["saves"][name]["float"] = value;
            Save();
        }

        public void SaveString(string name, string value)
        {
            CoreHelper.Log($"Saving {name} > {value}");
            if (string.IsNullOrEmpty(value))
                return;
            storySavesJSON["saves"][name]["string"] = value;
            Save();
        }

        public void SaveNode(string name, JSONNode value)
        {
            CoreHelper.Log($"Saving {name} > {value}");
            if (value == null)
                return;
            storySavesJSON["saves"][name][value.IsArray ? "array" : "object"] = value;
            Save();
        }

        public bool LoadBool(string name, bool defaultValue) => storySavesJSON["saves"][name] == null || storySavesJSON["saves"][name]["bool"] == null ? defaultValue : storySavesJSON["saves"][name]["bool"].AsBool;
        public int LoadInt(string name, int defaultValue) => storySavesJSON["saves"][name] == null || storySavesJSON["saves"][name]["int"] == null ? defaultValue : storySavesJSON["saves"][name]["int"].AsInt;
        public float LoadFloat(string name, float defaultValue) => storySavesJSON["saves"][name] == null || storySavesJSON["saves"][name]["float"] == null ? defaultValue : storySavesJSON["saves"][name]["float"].AsFloat;
        public string LoadString(string name, string defaultValue) => storySavesJSON["saves"][name] == null || storySavesJSON["saves"][name]["string"] == null ? defaultValue : storySavesJSON["saves"][name]["string"].Value;
        public JSONNode LoadJSON(string name) => storySavesJSON["saves"][name] == null ? null : storySavesJSON["saves"][name]["array"] != null ? storySavesJSON["saves"][name]["array"] : storySavesJSON["saves"][name]["object"] != null ? storySavesJSON["saves"][name]["object"] : null;

        public bool HasSave(string name) => storySavesJSON["saves"][name] != null;

        #region PAChat

        public void ClearChats()
        {
            storySavesJSON.Remove("chat");
            Save();
        }

        public List<string> ReadChats()
        {
            var list = new List<string>();
            if (storySavesJSON["chat"] != null)
                for (int i = 0; i < storySavesJSON["chat"].Count; i++)
                    list.Add(storySavesJSON["chat"][i]["text"]);

            return list;
        }

        public string ReadChatTime(int index) => storySavesJSON["chat"][index]["time"];
        public string ReadChatCharacter(int index) => storySavesJSON["chat"][index]["char"];
        public string ReadChatText(int index) => storySavesJSON["chat"][index]["text"];

        public void AddChat(string character, string chat, string time)
        {
            int index = storySavesJSON["chat"] == null ? 0 : storySavesJSON["chat"].Count;
            SetChat(index, character, chat, time);
        }

        public void SetChat(int index, string character, string chat, string time)
        {
            storySavesJSON["chat"][index]["time"] = time;
            storySavesJSON["chat"][index]["char"] = character;
            storySavesJSON["chat"][index]["text"] = chat;
            Save();
        }

        #endregion

        #endregion

        #region Play

        public void Play(string path) => StartCoroutine(IPlay(path));

        public void Play(int chapter, int level, bool bonus = false, bool skipCutscenes = false)
        {
            var storyLevel = (bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters)[chapter].levels[level];

            currentPlayingChapterIndex = chapter;
            currentPlayingLevelSequenceIndex = level;

            StartCoroutine(IPlay(storyLevel, skipCutscenes: skipCutscenes));
        }

        public IEnumerator IPlay(StoryMode.LevelSequence level, int cutsceneIndex = 0, bool skipCutscenes = false)
        {
            var path = level.filePath;
            bool isCutscene = false;

            int chapterIndex = LoadInt("Chapter", 0);
            int levelIndex = LoadInt($"DOC{(chapterIndex + 1).ToString("00")}Progress", 0);
            var completeString = $"DOC{(chapterIndex + 1).ToString("00")}_{(levelIndex + 1).ToString("00")}Complete";

            if (!skipCutscenes && cutsceneIndex >= 0 && cutsceneIndex < level.Count && level.Count > 1 && !LoadBool(completeString, false))
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
            if (path.EndsWith(".lsb"))
            {
                path = Path.GetDirectoryName(path).Replace("\\", "/") + "/";
                SetLevelEnd(level, isCutscene, cutsceneIndex);

                StartCoroutine(LevelManager.Play(new Level(path) { isStory = true }));
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

                SetLevelEnd(level, isCutscene, cutsceneIndex);

                if (!storyLevel.music)
                {
                    CoreHelper.LogError($"Music is null for some reason wtf");
                    return;
                }

                StartCoroutine(LevelManager.Play(storyLevel));
            }));

            yield break;
        }

        public IEnumerator IPlay(string path)
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
            if (path.EndsWith(".lsb"))
            {
                path = Path.GetDirectoryName(path).Replace("\\", "/") + "/";
                LevelManager.OnLevelEnd = () =>
                {
                    LevelManager.Clear();
                    Updater.OnLevelEnd();
                    UpdateCurrentLevelProgress(); // allow players to get a better rank

                    if (!ContinueStory)
                    {
                        CoreHelper.InStory = true;
                        LevelManager.OnLevelEnd = null;
                        ContinueStory = true;
                        SceneHelper.LoadInterfaceScene();
                        return;
                    }

                    int chapter = LoadInt("Chapter", 0);
                    int level = LoadInt($"DOC{(chapter + 1).ToString("00")}Progress", 0);
                    level++;
                    if (level >= StoryMode.Instance.chapters[chapter].levels.Count)
                    {
                        UnlockChapterAchievement(chapter);
                        chapter++;
                        level = 0;
                    }

                    chapter = Mathf.Clamp(chapter, 0, StoryMode.Instance.chapters.Count - 1);

                    SaveInt("Chapter", chapter);
                    SaveInt($"DOC{(chapter + 1).ToString("00")}Progress", level);

                    CoreHelper.InStory = true;
                    LevelManager.OnLevelEnd = null;
                    SceneHelper.LoadInterfaceScene();
                };

                StartCoroutine(LevelManager.Play(new Level(path) { isStory = true }));
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

                LevelManager.OnLevelEnd = () =>
                {
                    LevelManager.Clear();
                    Updater.OnLevelEnd();
                    UpdateCurrentLevelProgress(); // allow players to get a better rank

                    int chapter = LoadInt("Chapter", 0);
                    int level = LoadInt($"DOC{(chapter + 1).ToString("00")}Progress", 0);
                    level++;
                    if (level >= StoryMode.Instance.chapters[chapter].levels.Count)
                    {
                        chapter++;
                        level = 0;
                    }

                    chapter = Mathf.Clamp(chapter, 0, StoryMode.Instance.chapters.Count - 1);

                    SaveInt("Chapter", chapter);
                    SaveInt($"DOC{(chapter + 1).ToString("00")}Progress", level);

                    if (!ContinueStory)
                    {
                        CoreHelper.InStory = true;
                        LevelManager.OnLevelEnd = null;
                        ContinueStory = true;
                        SceneHelper.LoadInterfaceScene();
                        return;
                    }

                    CoreHelper.InStory = true;
                    LevelManager.OnLevelEnd = null;
                    SceneHelper.LoadInterfaceScene();
                };

                if (!storyLevel.music)
                {
                    CoreHelper.LogError($"Music is null for some reason wtf");
                    return;
                }

                StartCoroutine(LevelManager.Play(storyLevel));
            }));

            yield break;
        }

        public IEnumerator IPlayOnce(string path)
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
            if (path.EndsWith(".lsb"))
            {
                path = Path.GetDirectoryName(path).Replace("\\", "/") + "/";
                LevelManager.OnLevelEnd = () =>
                {
                    LevelManager.Clear();
                    Updater.OnLevelEnd();
                    UpdateCurrentLevelProgress(); // allow players to get a better rank

                    if (!ContinueStory)
                    {
                        CoreHelper.InStory = true;
                        LevelManager.OnLevelEnd = null;
                        ContinueStory = true;
                        SceneHelper.LoadInterfaceScene();
                        return;
                    }

                    int chapter = LoadInt("Chapter", 0);
                    int level = LoadInt($"DOC{(chapter + 1).ToString("00")}Progress", 0);
                    level++;
                    if (level >= StoryMode.Instance.chapters[chapter].levels.Count)
                    {
                        UnlockChapterAchievement(chapter);
                        chapter++;
                        level = 0;
                    }

                    chapter = Mathf.Clamp(chapter, 0, StoryMode.Instance.chapters.Count - 1);

                    SaveInt("Chapter", chapter);
                    SaveInt($"DOC{(chapter + 1).ToString("00")}Progress", level);

                    CoreHelper.InStory = true;
                    LevelManager.OnLevelEnd = null;
                    SceneHelper.LoadInterfaceScene();
                };

                StartCoroutine(LevelManager.Play(new Level(path) { isStory = true }));
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

                LevelManager.OnLevelEnd = () =>
                {
                    LevelManager.Clear();
                    Updater.OnLevelEnd();
                    UpdateCurrentLevelProgress(); // allow players to get a better rank

                    int chapter = LoadInt("Chapter", 0);
                    int level = LoadInt($"DOC{(chapter + 1).ToString("00")}Progress", 0);
                    level++;
                    if (level >= StoryMode.Instance.chapters[chapter].levels.Count)
                    {
                        chapter++;
                        level = 0;
                    }

                    chapter = Mathf.Clamp(chapter, 0, StoryMode.Instance.chapters.Count - 1);

                    SaveInt("Chapter", chapter);
                    SaveInt($"DOC{(chapter + 1).ToString("00")}Progress", level);

                    if (!ContinueStory)
                    {
                        CoreHelper.InStory = true;
                        LevelManager.OnLevelEnd = null;
                        ContinueStory = true;
                        SceneHelper.LoadInterfaceScene();
                        return;
                    }

                    CoreHelper.InStory = true;
                    LevelManager.OnLevelEnd = null;
                    SceneHelper.LoadInterfaceScene();
                };

                if (!storyLevel.music)
                {
                    CoreHelper.LogError($"Music is null for some reason wtf");
                    return;
                }

                StartCoroutine(LevelManager.Play(storyLevel));
            }));

            yield break;
        }

        void UnlockChapterAchievement(int chapter) => AchievementManager.inst.UnlockAchievement($"story_doc{(chapter + 1).ToString("00")}_complete");

        void SetLevelEnd(StoryMode.LevelSequence level, bool isCutscene = false, int cutsceneIndex = 0)
        {
            LevelManager.OnLevelEnd = () =>
            {
                LevelManager.Clear();
                Updater.UpdateObjects(false);
                if (!isCutscene)
                    UpdateCurrentLevelProgress(); // allow players to get a better rank

                int chapterIndex = currentPlayingChapterIndex;
                int levelIndex = currentPlayingLevelSequenceIndex;

                var completeString = $"DOC{(chapterIndex + 1).ToString("00")}_{(levelIndex + 1).ToString("00")}Complete";

                if (!isCutscene)
                    SaveBool(completeString, true);

                if (!ContinueStory)
                {
                    CoreHelper.InStory = true;
                    LevelManager.OnLevelEnd = null;
                    ContinueStory = true;
                    SceneHelper.LoadInterfaceScene();
                    return;
                }

                cutsceneIndex++;
                if (cutsceneIndex < level.Count)
                {
                    StartCoroutine(IPlay(level, cutsceneIndex));
                    return;
                }

                levelIndex++;
                SaveInt($"DOC{(chapterIndex + 1).ToString("00")}Progress", levelIndex);
                if (levelIndex >= StoryMode.Instance.chapters[chapterIndex].levels.Count)
                {
                    UnlockChapterAchievement(chapterIndex);
                    chapterIndex++;
                    levelIndex = 0;
                }

                if (chapterIndex >= StoryMode.Instance.chapters.Count)
                {
                    SoundManager.inst.PlaySound(DefaultSounds.loadsound);
                    CoreHelper.InStory = false;
                    LevelManager.OnLevelEnd = null;
                    SceneHelper.LoadScene(SceneName.Main_Menu);
                    return;
                }

                chapterIndex = Mathf.Clamp(chapterIndex, 0, StoryMode.Instance.chapters.Count - 1);

                SaveInt("Chapter", chapterIndex);
                SaveInt($"DOC{(chapterIndex + 1).ToString("00")}Progress", levelIndex);

                CoreHelper.InStory = true;
                LevelManager.OnLevelEnd = null;
                InterfaceManager.inst.onReturnToStoryInterface = () => InterfaceManager.inst.Parse(level.returnInterface);
                SceneHelper.LoadInterfaceScene();
            };
        }

        #endregion
    }
}