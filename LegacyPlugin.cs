using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using BepInEx;
using HarmonyLib;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

using Version = BetterLegacy.Core.Data.Version;

namespace BetterLegacy
{
    /// <summary>
    /// Core plugin class.
    /// </summary>
    [BepInPlugin("com.mecha.betterlegacy", "Better Legacy", "1.8.8")]
    [BepInProcess("Project Arrhythmia.exe")]
    public class LegacyPlugin : BaseUnityPlugin
    {
        public static LegacyPlugin inst;
        public static string className = "[<color=#0E36FD>Better</color> <color=#4FBDD1>Legacy</color>] " + PluginInfo.PLUGIN_VERSION + "\n";
        public static readonly Harmony harmony = new Harmony("BetterLegacy");
        public static Version ModVersion => new Version(PluginInfo.PLUGIN_VERSION);

        public static List<BaseConfig> configs = new List<BaseConfig>();

        public static Sprite PALogoSprite { get; set; }
        public static Sprite PAVGLogoSprite { get; set; }
        public static Sprite LockSprite { get; set; }
        public static Sprite EmptyObjectSprite { get; set; }
        public static Sprite AtanPlaceholder { get; set; }

        public static JSONObject authData;
        public static Lang SplashText { get; set; }

        public static FPSCounter FPSCounter { get; set; }

        public static Prefab ExamplePrefab { get; set; }

        public static string LevelStartupPath { get; set; }

        public static bool CanEdit { get; set; } = true;

        public const string DATE_TIME_FORMAT = "yyyy-MM-dd_HH.mm.ss";

        public static Core.Threading.TickRunner MainTick { get; set; }

        void Awake()
        {
            inst = this;

            DateOpened = DateTime.Now;

            try
            {
                CoreHelper.Log("Init patches...");
                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Patching failed.\n{ex}"); // Catch for cases where patchers fail to work.
                System.Windows.Forms.MessageBox.Show($"Patching the game failed. This mod is only for the Legacy branch, which can be unlocked on Steam by using the code \"oldlegacy2020\". If you are using the Legacy branch, please send this log to RTMecha.");
                throw;
            } // Patch initialization

            try
            {
                CoreHelper.Log("Loading configs...");

                RTFile.CreateDirectory(RTFile.ApplicationDirectory + "profile");

                InitConfigs();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Configs failed to load.\n{ex}");
                System.Windows.Forms.MessageBox.Show($"Loading the configs failed. Is this because the files are corrupted? Check the profile folder to verify.");
                throw;
            } // Config initializations

            try
            {
                CoreHelper.Log("Loading profile...");

                ParseProfile();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Profile failed to load.\n{ex}");
            }

            try
            {
                CoreHelper.Log("Loading asset packs...");
                AssetPack.LoadAssetPacks();
                CustomObjectType.LoadObjectTypes();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Asset Packs failed to load.\n{ex}");
            } // Asset Pack loading

            try
            {
                CoreHelper.Log("Loading assets...");

                LegacyResources.GetKinoGlitch();
                LegacyResources.GetObjectMaterials();
                LegacyResources.GetGUIAssets();
                LegacyResources.GetEffects();
                LegacyResources.GetSayings();
                
                LockSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/lock{FileFormat.PNG.Dot()}"));
                EmptyObjectSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/empty{FileFormat.PNG.Dot()}"));
                AtanPlaceholder = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/atan-placeholder{FileFormat.PNG.Dot()}"));
                PALogoSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/pa_logo{FileFormat.PNG.Dot()}"));
                PAVGLogoSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/pa_logo_vg{FileFormat.PNG.Dot()}"));
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Assets failed to load.\n{ex}");
            } // Asset handling

            try
            {
                CoreHelper.Log("Creating prefabs...");

                EditorPrefabHolder.Init();
                CorePrefabHolder.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to initialize Unity Prefab Holder.\n{ex}");
            } // Prefab Holder initializations

            try
            {
                CoreHelper.Log("Setting up Editor Themes...");
                EditorThemeManager.LoadEditorThemes();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to initialize Editor Themes.\n{ex}");
                System.Windows.Forms.MessageBox.Show($"Editor themes failed to load.");
                throw;
            } // Editor themes loading

            // For loading rounded sprites before Config Manager UI.
            SpriteHelper.Init();

            try
            {
                CoreHelper.Log("Init ConfigManager...");

                ConfigManager.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Config Manager failed to generate.\n{ex}");
            } // Config Manager initialization

            try
            {
                CoreHelper.Log("Init Tooltips...");

                TooltipHelper.InitTooltips();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to init tooltips due to an exception: {ex}");
            } // Init tooltips

            try
            {
                var authPath = Path.Combine(Application.persistentDataPath, "auth.json");
                if (RTFile.FileExists(authPath))
                    authData = JSON.Parse(RTFile.ReadFromFile(authPath)).AsObject;

            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to load auth. {ex}");
            } // auth

            LoadSplashText();

            try
            {
                FPSCounter = Creator.NewPersistentGameObject("FPS Counter").AddComponent<FPSCounter>();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to create FPS Counter. {ex}");
            } // FPS Counter

            try
            {
                if (AccessTools.TypeByName("EditorOnStartup.Plugin") != null)
                {
                    ModCompatibility.EditorOnStartupInstalled = true;
                    ModCompatibility.ShouldLoadExample = true;
                    SceneHelper.CurrentScene = "Editor";
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to update Current Scene. {ex}");
            }

            MainTick = new Core.Threading.TickRunner();
            Core.Threading.SyncContextUtil.Init();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
        }

        void Update()
        {
            EditorThemeManager.Update(); // Checks if editor scene has been exited, if it has it'll clear the editor theme elements.

            Application.runInBackground = CoreConfig.Instance.RunInBackground.Value; // If the game should continue playing in the background while you don't have the app focused.

            DebugInfo.Update();

            if (CoreConfig.Instance.PhysicsUpdateMatchFramerate.Value)
                Time.fixedDeltaTime = Time.deltaTime;
            else
                Time.fixedDeltaTime = 0.02f; // default

            try
            {
                CoreHelper.IsUsingInputField = LSHelpers.IsUsingInputField();
            }
            catch
            {

            }

            MainTick?.OnTick();

            if (CoreHelper.IsUsingInputField)
                return;

            if (CoreHelper.InEditor && Input.GetKeyDown(EditorConfig.Instance.EditorCamToggle.Value))
            {
                RTEditor.inst.Freecam = !RTEditor.inst.Freecam; // Enables / disables editor camera via the custom keybind.
                EditorManager.inst.DisplayNotification($"{(RTEditor.inst.Freecam ? "Enabled" : "Disabled")} editor freecam.", 2f);
            }

            if (Input.GetKeyDown(EventsConfig.Instance.ShowGUIToggle.Value))
            {
                EventsConfig.Instance.ShowGUI.Value = !EventsConfig.Instance.ShowGUI.Value; // Enabled / disables the Players / GUI via the custom keybind.
                if (CoreHelper.InEditor)
                    EditorManager.inst.DisplayNotification($"{(EventsConfig.Instance.ShowGUI.Value ? "Show" : "Hide")} GUI & Players", 2f);
            }

            if (Input.GetKeyDown(CoreConfig.Instance.OpenPAFolder.Value))
                RTFile.OpenInFileBrowser.Open(RTFile.ApplicationDirectory); // Opens the PA Application folder via the custom keybind.

            if (Input.GetKeyDown(CoreConfig.Instance.OpenPAPersistentFolder.Value))
                RTFile.OpenInFileBrowser.Open(RTFile.PersistentApplicationDirectory); // Opens the PA LocalLow folder via the custom keybind.

            if (Input.GetKeyDown(CoreConfig.Instance.DebugInfoToggleKey.Value))
                CoreConfig.Instance.DebugInfo.Value = !CoreConfig.Instance.DebugInfo.Value; // Enables / disables the debug info via the custom keybind.
        }

        void InitConfigs()
        {
            configs.Add(new CoreConfig());
            configs.Add(new ArcadeConfig());
            configs.Add(new EditorConfig());
            configs.Add(new EventsConfig());
            configs.Add(new PlayerConfig());
            configs.Add(new MenuConfig());
            configs.Add(new ExampleConfig());
        }

        public static void LoadSplashText()
        {
            try
            {
                var splashTextPath = AssetPack.GetFile("core/splashes.txt");
                if (RTFile.FileExists(splashTextPath))
                {
                    var splashes = RTString.GetLines(RTFile.ReadFromFile(splashTextPath));
                    var splashIndex = UnityEngine.Random.Range(0, splashes.Length);
                    var spashText = splashes[splashIndex];
                    SplashText = spashText.StartsWith("{") && spashText.EndsWith("}") ? Lang.Parse(JSON.Parse(spashText)) : spashText;
                }
                else
                    SplashText = string.Empty;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to load splash text. {ex}");
            } // Splash text
        }

        #region Profile

        /// <summary>
        /// The ID of the user on the BetterLegacy server.
        /// </summary>
        public static string UserID => authData == null ? string.Empty : authData["id"];

        /// <summary>
        /// The date time the game was opened.
        /// </summary>
        public static DateTime DateOpened { get; private set; }

        /// <summary>
        /// Total amount of editor levels opened.
        /// </summary>
        public static int OpenedEditorLevelCount { get; set; }

        /// <summary>
        /// Total amount of arcade levels opened.
        /// </summary>
        public static int OpenedArcadeLevelCount { get; set; }

        /// <summary>
        /// Maximum amount of recent levels.
        /// </summary>
        public static int RecentLevelMaxCount { get; set; } = 20;

        /// <summary>
        /// Recently opened editor levels.
        /// </summary>
        public static List<LevelInfo> RecentEditorLevels { get; set; } = new List<LevelInfo>();

        /// <summary>
        /// Registers a recently opened editor level.
        /// </summary>
        /// <param name="level">Level to register.</param>
        public static void AddRecentEditorLevel(Level level)
        {
            OpenedEditorLevelCount++;
            if (!level || !CoreConfig.Instance.StoreRecentLevels.Value || RecentEditorLevels.Has(x => x.arcadeID == level.id))
            {
                SaveStats();
                return;
            }

            while (RecentEditorLevels.Count > RecentLevelMaxCount)
                RecentEditorLevels.RemoveAt(0);
            RecentEditorLevels.Add(LevelInfo.FromLevel(level));

            SaveStats();
        }

        /// <summary>
        /// Recently saved editor levels.
        /// </summary>
        public static List<LevelInfo> RecentSavedEditorLevels { get; set; } = new List<LevelInfo>();

        /// <summary>
        /// Registers a recently saved editor level.
        /// </summary>
        /// <param name="level">Level to register.</param>
        public static void AddRecentSavedEditorLevel(Level level)
        {
            if (!level || !CoreConfig.Instance.StoreRecentLevels.Value || RecentSavedEditorLevels.Has(x => x.arcadeID == level.id))
                return;

            while (RecentSavedEditorLevels.Count > RecentLevelMaxCount)
                RecentSavedEditorLevels.RemoveAt(0);
            RecentSavedEditorLevels.Add(LevelInfo.FromLevel(level));

            SaveStats();
        }

        /// <summary>
        /// Recently opened arcade levels.
        /// </summary>
        public static List<LevelInfo> RecentArcadeLevels { get; set; } = new List<LevelInfo>();

        /// <summary>
        /// Registers a recently opened arcade level.
        /// </summary>
        /// <param name="level">Level to register.</param>
        public static void AddRecentArcadeLevel(Level level)
        {
            OpenedArcadeLevelCount++;
            if (!level || !CoreConfig.Instance.StoreRecentLevels.Value || RecentArcadeLevels.Has(x => x.arcadeID == level.id))
            {
                SaveStats();
                return;
            }

            while (RecentArcadeLevels.Count > RecentLevelMaxCount)
                RecentArcadeLevels.RemoveAt(0);
            RecentArcadeLevels.Add(LevelInfo.FromLevel(level));

            SaveStats();
        }

        /// <summary>
        /// Clears the recent levels from the profile.
        /// </summary>
        public static void ClearRecentLevels()
        {
            RecentEditorLevels.Clear();
            RecentSavedEditorLevels.Clear();
            RecentArcadeLevels.Clear();
            SaveStats();
        }

        /// <summary>
        /// Saves the users profile.
        /// </summary>
        public static void SaveProfile()
        {
            var jn = Parser.NewJSONObject();

            jn["user_data"]["name"] = player.sprName;
            jn["user_data"]["spr_id"] = player.sprID;

            for (int i = 0; i < AchievementManager.globalAchievements.Count; i++)
            {
                jn["internal_achievements"][i]["id"] = AchievementManager.globalAchievements[i].id;
                jn["internal_achievements"][i]["unlocked"] = AchievementManager.globalAchievements[i].unlocked;
            }
            
            for (int i = 0; i < AchievementManager.unlockedCustomAchievements.Count; i++)
            {
                jn["internal_achievements"][i]["id"] = AchievementManager.globalAchievements[i].id;
                jn["internal_achievements"][i]["unlocked"] = AchievementManager.globalAchievements[i].unlocked;
            }

            int num = 0;
            foreach (var keyValuePair in AchievementManager.unlockedCustomAchievements)
            {
                jn["custom_achievements"][num]["id"] = keyValuePair.Key;
                jn["custom_achievements"][num]["unlocked"] = keyValuePair.Value;
                num++;
            }

            for (int i = 0; i < AssetPack.Settings.Count; i++)
            {
                jn["asset_packs"][i]["id"] = AssetPack.Settings[i].id;
                jn["asset_packs"][i]["enabled"] = AssetPack.Settings[i].enabled;
            }

            if (player.memory != null)
                jn["memory"] = player.memory;

            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile");
            RTFile.CreateDirectory(path);
            RTFile.WriteToFile(RTFile.CombinePaths(path, "profile.sep"), jn.ToString());

            SaveStats();
        }

        /// <summary>
        /// Loads the users profile.
        /// </summary>
        public static void ParseProfile()
        {
            LoadStats();

            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile");
            if (!RTFile.DirectoryExists(path))
                return;

            string rawProfileJSON = RTFile.ReadFromFile(RTFile.CombinePaths(path, "profile.sep"));

            if (string.IsNullOrEmpty(rawProfileJSON))
                return;

            var jn = JSON.Parse(rawProfileJSON);

            if (!string.IsNullOrEmpty(jn["user_data"]["name"]))
                player.sprName = jn["user_data"]["name"];

            if (!string.IsNullOrEmpty(jn["user_data"]["spr-id"]))
                player.sprID = jn["user_data"]["spr-id"];
            if (!string.IsNullOrEmpty(jn["user_data"]["spr_id"]))
                player.sprID = jn["user_data"]["spr_id"];

            try
            {
                AchievementManager.unlockedGlobalAchievements.Clear();
                if (jn["internal_achievements"] != null)
                {
                    for (int i = 0; i < jn["internal_achievements"].Count; i++)
                        AchievementManager.unlockedGlobalAchievements[jn["internal_achievements"][i]["id"]] = jn["internal_achievements"][i]["unlocked"].AsBool;
                }
                AchievementManager.unlockedCustomAchievements.Clear();
                if (jn["custom_achievements"] != null)
                    for (int i = 0; i < jn["custom_achievements"].Count; i++)
                        AchievementManager.unlockedCustomAchievements[jn["custom_achievements"][i]["id"]] = jn["custom_achievements"][i]["unlocked"].AsBool;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Exception: {ex}");
            }

            AssetPack.Settings.Clear();

            for (int i = 0; i < jn["asset_packs"].Count; i++)
            {
                var jnAssetPack = jn["asset_packs"][i];
                AssetPack.Settings.Add(new AssetPack.UserSettings(jnAssetPack["id"], jnAssetPack["enabled"].AsBool));
            }

            if (jn["memory"] != null)
                player.memory = jn["memory"];
            else
                player.memory = Parser.NewJSONObject();
        }

        /// <summary>
        /// Saves game stats.
        /// </summary>
        public static void SaveStats()
        {
            try
            {
                var jn = Parser.NewJSONObject();

                jn["date_opened"] = DateOpened.ToString(DATE_TIME_FORMAT);
                jn["opened_editor_level_count"] = OpenedEditorLevelCount;
                jn["opened_arcade_level_count"] = OpenedArcadeLevelCount;
                for (int i = 0; i < RecentEditorLevels.Count; i++)
                    jn["recent_editor_levels"][i] = RecentEditorLevels[i].ToJSON();
                for (int i = 0; i < RecentSavedEditorLevels.Count; i++)
                    jn["recent_saved_editor_levels"][i] = RecentSavedEditorLevels[i].ToJSON();
                for (int i = 0; i < RecentArcadeLevels.Count; i++)
                    jn["recent_arcade_levels"][i] = RecentArcadeLevels[i].ToJSON();

                RTFile.WriteToFile(RTFile.CombinePaths(RTFile.ApplicationDirectory, "stats.json"), jn.ToString(3));
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to save info file. {ex}");
            }
        }

        /// <summary>
        /// Loads game stats.
        /// </summary>
        public static void LoadStats()
        {
            try
            {
                if (!RTFile.TryReadFromFile(RTFile.CombinePaths(RTFile.ApplicationDirectory, "stats.json"), out string file))
                    return;

                var jn = JSON.Parse(file);

                OpenedEditorLevelCount = jn["opened_editor_level_count"].AsInt;
                OpenedArcadeLevelCount = jn["opened_arcade_level_count"].AsInt;
                RecentEditorLevels.Clear();
                for (int i = 0; i < jn["recent_editor_levels"].Count; i++)
                    RecentEditorLevels.Add(LevelInfo.Parse(jn["recent_editor_levels"][i], i));
                RecentSavedEditorLevels.Clear();
                for (int i = 0; i < jn["recent_saved_editor_levels"].Count; i++)
                    RecentSavedEditorLevels.Add(LevelInfo.Parse(jn["recent_saved_editor_levels"][i], i));
                RecentArcadeLevels.Clear();
                for (int i = 0; i < jn["recent_arcade_levels"].Count; i++)
                    RecentArcadeLevels.Add(LevelInfo.Parse(jn["recent_arcade_levels"][i], i));
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to load info file. {ex}");
            }
        }

        public static List<Universe> universes = new List<Universe>
        {
            new Universe("Axiom Nexus", Universe.UniDes.Chardax, "000"),
        };

        public static User player = new User("Player", UnityEngine.Random.Range(0, ulong.MaxValue).ToString(), new Universe(Universe.UniDes.MUS));

        public class User : Exists
        {
            public User(string sprName, string sprID, Universe universe)
            {
                this.sprName = sprName;
                this.sprID = sprID;
                this.universe = universe;
            }

            public string sprName = "Null";
            public string sprID = "0";
            public Universe universe;

            public JSONNode memory;
        }

        public class Universe : Exists
        {
            public Universe()
            {
                uniDes = (UniDes)UnityEngine.Random.Range(0, 3);
                uniNum = string.Format("{0:000}", UnityEngine.Random.Range(0, int.MaxValue));

                for (int i = 0; i < UnityEngine.Random.Range(0, 10); i++)
                {
                    timelines.Add(new Timeline(UnityEngine.Random.Range(0f, 9999999f)));
                }
            }

            public Universe(UniDes uniDes)
            {
                this.uniDes = uniDes;
                uniNum = string.Format("{0:000}", UnityEngine.Random.Range(0, int.MaxValue));

                Debug.LogFormat("{0}UniNum: {1}", className, uniNum);

                timelines = new List<Timeline>();
                for (int i = 0; i < UnityEngine.Random.Range(0, 10); i++)
                {
                    timelines.Add(new Timeline(UnityEngine.Random.Range(0f, 9999999f)));
                }

                Debug.LogFormat("{0}Timeline Count: {1}", className, timelines.Count);
            }

            public Universe(string name, UniDes uniDes, string uniNum)
            {
                this.name = name;
                this.uniDes = uniDes;
                this.uniNum = uniNum;
            }

            public Universe(UniDes _unidDes, string _uniNum)
            {
                uniDes = _unidDes;
                uniNum = _uniNum;
            }

            public string name;
            public UniDes uniDes;
            public string uniNum = "000";
            public List<Timeline> timelines;

            public enum UniDes
            {
                Chardax,
                Genark,
                Archmo,
                MUS
            }

            public class Timeline
            {
                public Timeline(float _frq)
                {
                    fq = _frq;
                }

                public float fq;
            }
        }

        #endregion
    }
}
