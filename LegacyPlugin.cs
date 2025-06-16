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
    [BepInPlugin("com.mecha.betterlegacy", "Better Legacy", "1.7.1")]
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

        void Awake()
        {
            inst = this;

            try
            {
                CoreHelper.Log("Init patches...");
                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Patching failed.\n{ex}"); // Catch for cases where patchers fail to work.
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
                throw;
            } // Config initializations

            try
            {
                CoreHelper.Log("Loading assets...");

                LegacyResources.GetKinoGlitch();
                LegacyResources.GetObjectMaterials();
                LegacyResources.GetGUIAssets();
                LegacyResources.GetEffects();
                LegacyResources.GetSayings();
                
                LockSprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}lock.png");
                EmptyObjectSprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_empty.png");
                AtanPlaceholder = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}atan-placeholder.png");
                PALogoSprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}pa_logo.png");
                PAVGLogoSprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}pa_logo_vg.png");
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Blur materials failed to load.\n{ex}");
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
            
            try
            {
                var splashTextPath = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}splashes.txt";
                if (RTFile.FileExists(splashTextPath))
                {
                    var splashes = RTString.GetLines(RTFile.ReadFromFile(splashTextPath));
                    var splashIndex = UnityEngine.Random.Range(0, splashes.Length);
                    SplashText = Lang.Parse(splashes[splashIndex].StartsWith("{") && splashes[splashIndex].EndsWith("}") ? JSON.Parse(splashes[splashIndex]) : splashes[splashIndex]);
                }
                else
                    SplashText = string.Empty;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to load splash text. {ex}");
            } // Splash text

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
                Application.quitting += () =>
                {
                    if (CoreHelper.InEditor && EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading)
                        GameData.Current?.SaveData(RTFile.CombinePaths(RTFile.BasePath, "level-quit-backup.lsb"));
                };
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"On Exit methods failed to set.{ex}");
            } // Quit saves backup

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

            BetterLegacy.Core.Threading.TickRunner.Main = new Core.Threading.TickRunner();

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

            Core.Threading.TickRunner.Main?.OnTick();

            if (CoreHelper.IsUsingInputField)
                return;

            if (CoreHelper.InEditor && Input.GetKeyDown(EventsConfig.Instance.EditorCamToggle.Value))
            {
                EventsConfig.Instance.EditorCamEnabled.Value = !EventsConfig.Instance.EditorCamEnabled.Value; // Enables / disables editor camera via the custom keybind.
                if (CoreHelper.InEditor)
                    EditorManager.inst.DisplayNotification($"{(EventsConfig.Instance.EditorCamEnabled.Value ? "Enabled" : "Disabled")} editor freecam.", 2f);
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

        #region Profile

        public static void SaveProfile()
        {
            var jn = JSON.Parse("{}");

            jn["user_data"]["name"] = player.sprName;
            jn["user_data"]["spr-id"] = player.sprID;

            for (int i = 0; i < AchievementManager.globalAchievements.Count; i++)
            {
                jn["internal_achievements"][i]["id"] = AchievementManager.globalAchievements[i].id;
                jn["internal_achievements"][i]["unlocked"] = AchievementManager.globalAchievements[i].unlocked;
            }

            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile");
            RTFile.CreateDirectory(path);
            RTFile.WriteToFile(RTFile.CombinePaths(path, "profile.sep"), jn.ToString());
        }

        public static void ParseProfile()
        {
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

            try
            {
                AchievementManager.unlockedGlobalAchievements.Clear();
                if (jn["internal_achievements"] != null)
                {
                    for (int i = 0; i < jn["internal_achievements"].Count; i++)
                        AchievementManager.unlockedGlobalAchievements[jn["internal_achievements"][i]["id"]] = jn["internal_achievements"][i]["unlocked"].AsBool;
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Exception: {ex}");
            }
        }

        public static List<Universe> universes = new List<Universe>
        {
            new Universe("Axiom Nexus", Universe.UniDes.Chardax, "000"),
        };

        public static User player = new User("Player", UnityEngine.Random.Range(0, ulong.MaxValue).ToString(), new Universe(Universe.UniDes.MUS));

        public class User
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
        }

        public class Universe
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
