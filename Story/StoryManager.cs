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

namespace BetterLegacy.Story
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager inst;

        public static string StoryAssetsPath => $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Story/";

        public AssetBundle assets;

        public bool Loaded { get; set; }

        public bool AssetBundlesLoaded => assets != null;

        public bool ContinueStory { get; set; } = true;

        /// <summary>
        /// The default chapter rank requirement for "bonuses" to be unlocked. In this case, the player needs to get higher than an A rank (S / SS rank).
        /// </summary>
        public const int CHAPTER_RANK_REQUIREMENT = 1;

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

        #region Save File

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
            if (LevelManager.CurrentLevel is not StoryLevel storyLevel)
                return;

            CoreHelper.Log($"Setting Player Data");

            // TODO: Implement achievement system (not the Steam one, the custom one)
            //if (Saves.Where(x => x.Completed).Count() >= 100)
            //{
            //    SteamWrapper.inst.achievements.SetAchievement("GREAT_TESTER");
            //}

            //if (PlayerManager.IsZenMode || PlayerManager.IsPractice)
            //    return;

            var makeNewPlayerData = storyLevel.playerData == null;
            if (makeNewPlayerData)
                storyLevel.playerData = new LevelManager.PlayerData { ID = storyLevel.id, LevelName = storyLevel.metadata?.beatmap?.name, };

            CoreHelper.Log($"Updating save data\n" +
                $"New Player Data = {makeNewPlayerData}\n" +
                $"Deaths [OLD = {storyLevel.playerData.Deaths} > NEW = {GameManager.inst.deaths.Count}]\n" +
                $"Hits: [OLD = {storyLevel.playerData.Hits} > NEW = {GameManager.inst.hits.Count}]\n" +
                $"Boosts: [OLD = {storyLevel.playerData.Boosts} > NEW = {LevelManager.BoostCount}]");

            storyLevel.playerData.Update(GameManager.inst.deaths.Count, GameManager.inst.hits.Count, LevelManager.BoostCount, true);

            if (Saves.TryFindIndex(x => x.ID == storyLevel.id, out int saveIndex))
                Saves[saveIndex] = storyLevel.playerData;
            else
                Saves.Add(storyLevel.playerData);

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
            catch (System.Exception ex)
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

        #endregion

        #region Play

        public void Clear(bool unloadAllLoadedObjects = true)
        {
            if (assets)
                assets.Unload(unloadAllLoadedObjects);
            assets = null;

            Loaded = false;
        }

        public void Play(string path) => StartCoroutine(IPlay(path));

        public void Play(int chapter, int level, bool bonus = false, bool skipCutscenes = false)
        {
            var storyLevel = (bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters)[chapter].levels[level];

            StartCoroutine(IPlay(storyLevel, skipCutscenes: skipCutscenes));
        }

        public IEnumerator IPlay(StoryMode.LevelSequence level, int cutsceneIndex = 0, bool skipCutscenes = false)
        {
            if (AssetBundlesLoaded)
                Clear();

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
                SceneManager.inst.LoadScene("Main Menu");
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

            yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAssetBundle($"file://{path}", assetBundle =>
            {
                assets = assetBundle;
            }));

            Loaded = true;

            CoreHelper.InStory = true;
            StoryLevel storyLevel = LoadCurrentLevel();

            if (storyLevel == null)
            {
                LevelManager.OnLevelEnd = null;
                SceneManager.inst.LoadScene("Interface");
                yield break;
            }

            SetLevelEnd(level, isCutscene, cutsceneIndex);

            if (!storyLevel.music)
            {
                CoreHelper.LogError($"Music is null for some reason wtf");
                yield break;
            }

            StartCoroutine(LevelManager.Play(storyLevel));

            yield break;
        }

        public IEnumerator IPlay(string path)
        {
            if (AssetBundlesLoaded)
                Clear();

            if (!RTFile.FileExists(path))
            {
                CoreHelper.LogError($"File \'{path}\' does not exist.");
                SoundManager.inst.PlaySound(DefaultSounds.Block);
                Loaded = false;
                CoreHelper.InStory = false;
                LevelManager.OnLevelEnd = null;
                SceneManager.inst.LoadScene("Main Menu");
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
                        SceneManager.inst.LoadScene("Interface");
                        return;
                    }

                    CoreHelper.InStory = true;
                    LevelManager.OnLevelEnd = null;
                    SceneManager.inst.LoadScene("Interface");
                };


                StartCoroutine(LevelManager.Play(new Level(path) { isStory = true }));
                yield break;
            }

            yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAssetBundle($"file://{path}", assetBundle =>
            {
                assets = assetBundle;
            }));

            Loaded = true;

            CoreHelper.InStory = true;
            StoryLevel storyLevel = LoadCurrentLevel();

            if (storyLevel == null)
            {
                LevelManager.OnLevelEnd = null;
                SceneManager.inst.LoadScene("Interface");
                yield break;
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
                    SceneManager.inst.LoadScene("Interface");
                    return;
                }

                CoreHelper.InStory = true;
                LevelManager.OnLevelEnd = null;
                SceneManager.inst.LoadScene("Interface");
            };

            if (!storyLevel.music)
            {
                CoreHelper.LogError($"Music is null for some reason wtf");
                yield break;
            }

            StartCoroutine(LevelManager.Play(storyLevel));

            yield break;
        }

        void SetLevelEnd(StoryMode.LevelSequence level, bool isCutscene = false, int cutsceneIndex = 0)
        {
            LevelManager.OnLevelEnd = () =>
            {
                LevelManager.Clear();
                Updater.UpdateObjects(false);
                UpdateCurrentLevelProgress(); // allow players to get a better rank

                int chapterIndex = LoadInt("Chapter", 0);
                int levelIndex = LoadInt($"DOC{(chapterIndex + 1).ToString("00")}Progress", 0);

                var completeString = $"DOC{(chapterIndex + 1).ToString("00")}_{(levelIndex + 1).ToString("00")}Complete";
                if (!isCutscene)
                    SaveBool(completeString, true);

                cutsceneIndex++;
                if (cutsceneIndex < level.Count)
                {
                    StartCoroutine(IPlay(level, cutsceneIndex));
                    return;
                }

                levelIndex++;
                if (levelIndex >= StoryMode.Instance.chapters[chapterIndex].levels.Count)
                {
                    chapterIndex++;
                    levelIndex = 0;
                }

                if (chapterIndex >= StoryMode.Instance.chapters.Count)
                {
                    SoundManager.inst.PlaySound(DefaultSounds.loadsound);
                    CoreHelper.InStory = false;
                    LevelManager.OnLevelEnd = null;
                    SceneManager.inst.LoadScene("Main Menu");
                    return;
                }

                chapterIndex = Mathf.Clamp(chapterIndex, 0, StoryMode.Instance.chapters.Count - 1);

                SaveInt("Chapter", chapterIndex);
                SaveInt($"DOC{(chapterIndex + 1).ToString("00")}Progress", levelIndex);

                if (!ContinueStory)
                {
                    CoreHelper.InStory = true;
                    LevelManager.OnLevelEnd = null;
                    ContinueStory = true;
                    SceneManager.inst.LoadScene("Interface");
                    return;
                }

                CoreHelper.InStory = true;
                LevelManager.OnLevelEnd = null;
                SceneManager.inst.LoadScene("Interface");
            };
        }

        public StoryLevel LoadCurrentLevel()
        {
            var icon = assets.LoadAsset<Sprite>($"cover.jpg");
            var song = assets.LoadAsset<AudioClip>($"song.ogg");
            var levelJSON = assets.LoadAsset<TextAsset>($"level.json");
            var metadataJSON = assets.LoadAsset<TextAsset>($"metadata.json");
            var players = assets.LoadAsset<TextAsset>($"players.json");

            if (!song)
                return null;

            var metadata = MetaData.Parse(JSON.Parse(metadataJSON.text), false);
            var storyLevel = new StoryLevel
            {
                id = metadata?.arcadeID,
                name = metadata?.beatmap?.name,
                icon = icon,
                music = song,
                json = levelJSON.text,
                metadata = metadata,
                jsonPlayers = players.text,
                videoClip = assets.Contains($"bg.mp4") ? assets.LoadAsset<VideoClip>($"bg.mp4") : null,
            };
            
            return storyLevel;
        }

        public void Load(int chapter, int level)
        {
            StartCoroutine(ILoad(chapter, level));
        }

        public IEnumerator ILoad(int chapter, int level)
        {
            if (AssetBundlesLoaded)
                Clear();

            var path = $"{StoryAssetsPath}doc{(chapter + 1).ToString("00")}_{(level + 1).ToString("00")}.asset";
            if (!RTFile.FileExists(path))
            {
                Loaded = false;
                yield break;
            }

            yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAssetBundle($"file://{path}", assetBundle =>
            {
                assets = assetBundle;
            }));

            Loaded = true;
        }

        #endregion
    }
}