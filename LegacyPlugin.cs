using BepInEx;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Patchers;
using HarmonyLib;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BetterLegacy
{
    /// <summary>
    /// Core plugin class.
    /// </summary>
    [BepInPlugin("com.mecha.betterlegacy", "Better Legacy", "1.3.5")]
    [BepInProcess("Project Arrhythmia.exe")]
    public class LegacyPlugin : BaseUnityPlugin
    {
        public static LegacyPlugin inst;
        public static string className = "[<color=#0E36FD>Better</color> <color=#4FBDD1>Legacy</color>] " + PluginInfo.PLUGIN_VERSION + "\n";
        public static readonly Harmony harmony = new Harmony("BetterLegacy");
        public static Core.Version ModVersion => new Core.Version(PluginInfo.PLUGIN_VERSION);

        static EditorPrefabHolder editorPrefabHolder;
        static CorePrefabHolder corePrefabHolder;

        public static List<BaseConfig> configs = new List<BaseConfig>();

        public static Sprite PALogoSprite { get; set; }
        public static Sprite LockSprite { get; set; }
        public static Sprite EmptyObjectSprite { get; set; }
        public static Sprite AtanPlaceholder { get; set; }

        public static Material blur;
        public static Material GetBlur()
        {
            var assetBundle = AssetBundle.LoadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}objectmaterials.asset"); // Get AssetBundle from assets folder.
            var assetToLoad = assetBundle.LoadAsset<Material>("blur.mat"); // Load asset
            var blurMat = Instantiate(assetToLoad); // Instantiate so we can keep the material
            assetBundle.Unload(false); // Unloads AssetBundle

            return blurMat;
        }

        public static Shader blurColored;

        public static Shader analogGlitchShader;
        public static Material analogGlitchMaterial;
        public static Shader digitalGlitchShader;
        public static Material digitalGlitchMaterial;
        public static void GetKinoGlitch()
        {
            var assetBundle = AssetBundle.LoadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}effects.asset"); // Get AssetBundle from assets folder.
            analogGlitchMaterial = assetBundle.LoadAsset<Material>("analogglitchmaterial.mat"); // Load asset
            digitalGlitchMaterial = assetBundle.LoadAsset<Material>("digitalglitchmaterial.mat"); // Load asset
            analogGlitchShader = assetBundle.LoadAsset<Shader>("analogglitch.shader"); // Load asset
            digitalGlitchShader = assetBundle.LoadAsset<Shader>("digitalglitch.shader"); // Load asset
        }


        public static Prefab ExamplePrefab { get; set; }

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

                if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "profile"))
                    Directory.CreateDirectory(RTFile.ApplicationDirectory + "profile");

                configs.Add(new CoreConfig());
                configs.Add(new EditorConfig());
                configs.Add(new ArcadeConfig());
                configs.Add(new MenuConfig());
                configs.Add(new EventsConfig());
                configs.Add(new ModifiersConfig());
                configs.Add(new PlayerConfig());
                configs.Add(new ExampleConfig());
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Configs failed to load.\n{ex}");
                throw;
            } // Config initializations

            try
            {
                CoreHelper.Log("Loading assets...");

                blur = GetBlur();
                var assetBundle = AssetBundle.LoadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}shadercolored.asset");
                blurColored = assetBundle.LoadAsset<Shader>("simpleblur.shader");
                assetBundle.Unload(false);
                GetKinoGlitch();

                LockSprite = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}lock.png");
                EmptyObjectSprite = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_empty.png");
                AtanPlaceholder = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}atan-placeholder.png");
                PALogoSprite = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}pa_logo.png");
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Blur materials failed to load.\n{ex}");
            } // Asset handling

            try
            {
                CoreHelper.Log("Creating prefabs...");

                editorPrefabHolder = new EditorPrefabHolder();
                corePrefabHolder = new CorePrefabHolder();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to initialize Unity Prefab Holder.\n{ex}");
            } // Prefab Holder initializations

            try
            {
                CoreHelper.Log("Setting up Editor Themes...");

                EditorThemeManager.EditorThemes = new List<EditorThemeManager.EditorTheme>();

                var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_themes.lst"));

                for (int i = 0; i < jn["themes"].Count; i++)
                {
                    var dictionary = new Dictionary<ThemeGroup, Color>();
                    for (int j = 0; j < jn["themes"][i]["groups"].Count; j++)
                    {
                        var colorJN = jn["themes"][i]["groups"][j]["color"];
                        string name = jn["themes"][i]["groups"][j]["name"];
                        if (Enum.TryParse(name, out ThemeGroup group) && !dictionary.ContainsKey(group))
                            dictionary.Add(group, new Color(colorJN["r"].AsFloat, colorJN["g"].AsFloat, colorJN["b"].AsFloat, colorJN["a"].AsFloat));
                    }

                    EditorThemeManager.EditorThemes.Add(new EditorThemeManager.EditorTheme(jn["themes"][i]["name"], dictionary));
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to initialize Editor Themes.\n{ex}");
                throw;
            } // Editor themes loading

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

            // Hooks
            {
                CoreHelper.Log("Applying hooks...");

                ObjectManagerPatch.LevelTick += RTEventManager.OnLevelTick; // events need to update first
                ObjectManagerPatch.LevelTick += Updater.OnLevelTick; // objects update second
                ObjectManagerPatch.LevelTick += ModifiersManager.OnLevelTick; // modifiers update third
            }

            try
            {
                Application.quitting += () =>
                {
                    if (EditorManager.inst && EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading && DataManager.inst.gameData is GameData)
                    {
                        string str = RTFile.BasePath;
                        string modBackup = str + "level-quit-backup.lsb";
                        if (RTFile.FileExists(modBackup))
                            File.Delete(modBackup);

                        CoreHelper.StartCoroutine(ProjectData.Writer.SaveData(modBackup, GameData.Current));
                    }
                };
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"On Exit methods failed to set.{ex}");
            } // Quit saves backup

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
        }

        void Update()
        {
            EditorThemeManager.Update(); // Checks if editor scene has been exited, if it has it'll clear the editor theme elements.

            Application.runInBackground = CoreConfig.Instance.RunInBackground.Value; // If the game should continue playing in the background while you don't have the app focused.

            RTDebugger.Update();

            try
            {
                CoreHelper.IsUsingInputField = LSHelpers.IsUsingInputField();
            }
            catch
            {

            }

            if (CoreHelper.IsUsingInputField)
                return;

            if (Input.GetKeyDown(EventsConfig.Instance.EditorCamToggle.Value))
                EventsConfig.Instance.EditorCamEnabled.Value = !EventsConfig.Instance.EditorCamEnabled.Value; // Enables / disables editor camera via the custom keybind.

            if (Input.GetKeyDown(EventsConfig.Instance.ShowGUIToggle.Value))
                EventsConfig.Instance.ShowGUI.Value = !EventsConfig.Instance.ShowGUI.Value; // Enabled / disables the Players / GUI via the custom keybind.

            if (Input.GetKeyDown(CoreConfig.Instance.OpenPAFolder.Value))
                RTFile.OpenInFileBrowser.Open(RTFile.ApplicationDirectory); // Opens the PA Application folder via the custom keybind.

            if (Input.GetKeyDown(CoreConfig.Instance.OpenPAPersistentFolder.Value))
                RTFile.OpenInFileBrowser.Open(RTFile.PersistentApplicationDirectory); // Opens the PA LocalLow folder via the custom keybind.

            if (Input.GetKeyDown(CoreConfig.Instance.DebugInfoToggleKey.Value))
                CoreConfig.Instance.DebugInfo.Value = !CoreConfig.Instance.DebugInfo.Value; // Enables / disables the debug info via the custom keybind.
        }

        #region Profile

        public static void SaveProfile()
        {
            var jn = JSON.Parse("{}");

            jn["user_data"]["name"] = player.sprName;
            jn["user_data"]["spr-id"] = player.sprID;

            for (int i = 0; i < AchievementManager.achievements.Count; i++)
                jn["achievements"][i] = AchievementManager.achievements[i].ToJSON(true);

            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "profile"))
                Directory.CreateDirectory(RTFile.ApplicationDirectory + "profile");

            RTFile.WriteToFile("profile/profile.sep", jn.ToString());
        }

        public static void ParseProfile()
        {
            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "profile"))
                return;

            string rawProfileJSON = RTFile.ReadFromFile(RTFile.ApplicationDirectory + "profile/profile.sep");

            if (string.IsNullOrEmpty(rawProfileJSON))
                return;

            var jn = JSON.Parse(rawProfileJSON);

            if (!string.IsNullOrEmpty(jn["user_data"]["name"]))
                player.sprName = jn["user_data"]["name"];

            if (!string.IsNullOrEmpty(jn["user_data"]["spr-id"]))
                player.sprID = jn["user_data"]["spr-id"];

            try
            {
                if (jn["achievements"] == null)
                    return;

                AchievementManager.achievements.Clear();
                for (int i = 0; i < jn["achievements"].Count; i++)
                    AchievementManager.achievements.Add(Achievement.Parse(jn["achievements"][i]));
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
