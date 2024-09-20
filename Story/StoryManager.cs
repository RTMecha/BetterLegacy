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

namespace BetterLegacy.Story
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager inst;

        public static string StoryAssetsPath => $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Story/";
        public static string StoryAssetsURL => $"{AlephNetworkManager.ArcadeServerURL}api/story/download";

        public AssetBundle assets;

        public bool Loaded { get; set; }

        public bool AssetBundlesLoaded => assets != null;

        public int Chapter { get; set; }
        public int Level { get; set; }
        public bool ContinueStory { get; set; } = true;

        public Dictionary<int, int> ChapterCounts => new Dictionary<int, int>
        {
            { 0, 6 },
            { 1, 6 },
            { 2, 6 },
            { 3, 6 },
            { 4, 6 },
        };

        public int[] chaptersProgression;
        public static int CHAPTER_RANK_REQUIREMENT = 1; // test

        public List<List<string>> levelIDs = new List<List<string>>
        {
            new List<string>
            {
                "0603661088835365", // Granite
                "7376679616786413", // Ahead of the Curve
                "9784345755418661", // Super Gamer Girl 3D
                "9454675971710439", // Slime Boy Color
                "6698982684586290", // Node (Para)
                "4462948827770399", // ???
            },
            new List<string>
            {
                "3434489214197233", // RPM
            },
        };

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

        public static void Init() => new GameObject(nameof(StoryManager), typeof(StoryManager)).transform.SetParent(SystemManager.inst.transform);

        void Awake()
        {
            inst = this;
            Load();
        }

        public void Load()
        {
            storySavesJSON = JSON.Parse(RTFile.FileExists(StorySavesPath) ? RTFile.ReadFromFile(StorySavesPath) : "{}");
            Chapter = GetChapter();
            Level = GetLevel();

            if (storySavesJSON["chapters"] != null)
            {
                chaptersProgression = new int[storySavesJSON["chapters"].Count];
                for (int i = 0; i < storySavesJSON["chapters"].Count; i++)
                    chaptersProgression[i] = storySavesJSON["chapters"][i].AsInt;
            }
            else
                chaptersProgression = new int[ChapterCounts.Count];

            Saves.Clear();
            if (storySavesJSON["lvl"] != null)
                for (int i = 0; i < storySavesJSON["lvl"].Count; i++)
                {
                    Saves.Add(LevelManager.PlayerData.Parse(storySavesJSON["lvl"][i]));
                }
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
                storyLevel.playerData = new LevelManager.PlayerData { ID = storyLevel.id, LevelName = storyLevel.metadata?.LevelBeatmap?.name, };

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

        public void SaveChapterProgress()
        {
            storySavesJSON["chapters"] = new JSONArray();
            for (int i = 0; i < chaptersProgression.Length; i++)
                storySavesJSON["chapters"][i] = chaptersProgression[i];
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

        public bool IsBonus(string id) => id switch
        {
            "4462948827770399" => true,
            _ => false,
        };

        public void SetChapter(int chapter)
        {
            CoreHelper.Log($"Updating chapter {Chapter} > {chapter}");
            Chapter = chapter;
            storySavesJSON["doc"] = chapter;
            SaveChapterProgress();
            Save();
        }

        public void SetLevel(int level)
        {
            CoreHelper.Log($"Updating level {Level} > {level}");
            Level = Mathf.Clamp(level, 0, ChapterCounts[Chapter] - 1);

            if (Chapter < chaptersProgression.Length && chaptersProgression[Chapter] < level)
                chaptersProgression[Chapter] = level;

            storySavesJSON["level"] = level;
            SaveChapterProgress();
            Save();
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

        public int GetChapter() => storySavesJSON["doc"].AsInt;
        public int GetLevel() => storySavesJSON["level"].AsInt;
        public bool LoadBool(string name, bool defaultValue) => storySavesJSON["saves"][name] == null || storySavesJSON["saves"][name]["bool"] == null ? defaultValue : storySavesJSON["saves"][name]["bool"].AsBool;
        public int LoadInt(string name, int defaultValue) => storySavesJSON["saves"][name] == null || storySavesJSON["saves"][name]["int"] == null ? defaultValue : storySavesJSON["saves"][name]["int"].AsInt;
        public float LoadFloat(string name, float defaultValue) => storySavesJSON["saves"][name] == null || storySavesJSON["saves"][name]["float"] == null ? defaultValue : storySavesJSON["saves"][name]["float"].AsFloat;
        public string LoadString(string name, string defaultValue) => storySavesJSON["saves"][name] == null || storySavesJSON["saves"][name]["string"] == null ? defaultValue : storySavesJSON["saves"][name]["string"].Value;
        public JSONNode LoadJSON(string name) => storySavesJSON["saves"][name] == null ? null : storySavesJSON["saves"][name]["array"] != null ? storySavesJSON["saves"][name]["array"] : storySavesJSON["saves"][name]["object"] != null ? storySavesJSON["saves"][name]["object"] : null;

        public void Clear(bool unloadAllLoadedObjects = true)
        {
            if (assets)
                assets.Unload(unloadAllLoadedObjects);
            assets = null;

            Loaded = false;
        }

        public List<LevelManager.PlayerData> Saves { get; set; } = new List<LevelManager.PlayerData>();

        public IEnumerator Download(System.Action onComplete = null)
        {
            yield return CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadBytes(StoryAssetsURL, (float percentage) => { }, (byte[] bytes) =>
            {
                var directory = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Story/test";
                var zip = $"{directory}/story.zip";

                if (!RTFile.DirectoryExists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(zip, bytes);

                ZipFile.ExtractToDirectory(zip, directory);

                File.Delete(zip);
            }, (string onError) => { }));

            onComplete?.Invoke();

            yield break;
        }

        public IEnumerator Demo(bool clearInputs)
        {
            if (clearInputs || InputDataManager.inst.players.Any(x => x is not CustomPlayer))
            {
                InputDataManager.inst.players.Clear();
                InputDataManager.inst.players.Add(new CustomPlayer(true, 0, null));
            }

            Play();
            yield break;
        }

        public void Play() => StartCoroutine(IPlay());

        public IEnumerator IPlay()
        {
            CoreHelper.Log($"Playing story: doc{(Chapter + 1).ToString("00")}_{(Level + 1).ToString("00")}");
            yield return StartCoroutine(ILoad(Chapter, Level));

            if (!Loaded)
            {
                CoreHelper.InStory = false;
                LevelManager.OnLevelEnd = null;
                SceneManager.inst.LoadScene("Main Menu");
                yield break;
            }

            CoreHelper.InStory = true;
            StoryLevel storyLevel = LoadCurrentLevel();

            if (storyLevel == null)
            {
                CoreHelper.InStory = true;
                LevelManager.OnLevelEnd = null;
                SceneManager.inst.LoadScene("Interface");
                yield break;
            }

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
                    SceneManager.inst.LoadScene("Interface");
                    return;
                }

                var chapter = GetChapter();
                if (Level != 5 && ChapterCounts.TryGetValue(chapter, out int maxLevel) && Level + 1 >= maxLevel)
                {
                    SetChapter(chapter + 1);
                    SetLevel(0);
                }
                else if (!ChapterCounts.ContainsKey(chapter))
                {
                    CoreHelper.Log("You reached the end.");
                }
                else
                {
                    SetLevel(Level + 1);
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

        public StoryLevel LoadCurrentLevel()
        {
            //var name = $"doc{(chapter + 1).ToString("00")}_{(levelIndex + 1).ToString("00")}";
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
                name = metadata?.LevelBeatmap?.name,
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
    }
}