using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using System;
using BetterLegacy.Patchers;
using System.IO;
using BetterLegacy.Core.Optimization;
using SimpleJSON;
using System.Collections.Generic;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using BetterLegacy.Core.Managers;

namespace BetterLegacy
{
	[BepInPlugin("com.mecha.betterlegacy", "Better Legacy", "1.0.0")]
	[BepInProcess("Project Arrhythmia.exe")]
	public class LegacyPlugin : BaseUnityPlugin
	{
		public static LegacyPlugin inst;
		public static string className = "[<color=#0E36FD>Better</color> <color=#4FBDD1>Legacy</color>] " + PluginInfo.PLUGIN_VERSION + "\n";
		public static readonly Harmony harmony = new Harmony("BetterLegacy");
		public static Core.Version ModVersion => new Core.Version(PluginInfo.PLUGIN_VERSION);

		static CoreConfig coreConfig;
		static EditorConfig editorConfig;
		static ArcadeConfig arcadeConfig;
		static MenuConfig menuConfig;
		static EventsConfig eventsConfig;
		static ModifiersConfig modifiersConfig;
		static PlayerConfig playerConfig;
		static ExampleConfig exampleConfig;
		static EditorPrefabHolder editorPrefabHolder;

		public static Material blur;
		public static Material GetBlur()
		{
			var assetBundle = AssetBundle.LoadFromFile(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/objectmaterials.asset");
			var assetToLoad = assetBundle.LoadAsset<Material>("blur.mat");
			var blurMat = Instantiate(assetToLoad);
			assetBundle.Unload(false);

			return blurMat;
		}

		public static Shader blurColored;

		void Awake()
		{
			inst = this;

			try
			{
				harmony.PatchAll();
			}
			catch (Exception ex)
			{
				CoreHelper.LogError($"Patching failed.\n{ex}");
				throw;
			}

            try
			{
				coreConfig = new CoreConfig(Config);
				editorConfig = new EditorConfig(Config);
				arcadeConfig = new ArcadeConfig(Config);
				menuConfig = new MenuConfig(Config);
				eventsConfig = new EventsConfig(Config);
				modifiersConfig = new ModifiersConfig(Config);
				playerConfig = new PlayerConfig(Config);
				exampleConfig = new ExampleConfig(Config);
			}
            catch (Exception ex)
            {
				CoreHelper.LogError($"Configs failed to load.\n{ex}");
				throw;
            }

			try
			{
				blur = GetBlur();
				var assetBundle = AssetBundle.LoadFromFile(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/shadercolored.asset");
				blurColored = assetBundle.LoadAsset<Shader>("simpleblur.shader");
				assetBundle.Unload(false);
			}
			catch (Exception ex)
			{
				CoreHelper.LogError($"Blur materials failed to load.\n{ex}");
			}

			try
			{
				editorPrefabHolder = new EditorPrefabHolder();
			}
			catch (Exception ex)
			{
				CoreHelper.LogError($"Failed to initialize Unity Prefab Holder.\n{ex}");
			}

			try
			{
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
			}

			// Hooks
			{
				GameManagerPatch.LevelStart += Updater.OnLevelStart;
				GameManagerPatch.LevelEnd += Updater.OnLevelEnd;
				ObjectManagerPatch.LevelTick += ModifiersManager.OnLevelTick;
				ObjectManagerPatch.LevelTick += RTEventManager.OnLevelTick;
				ObjectManagerPatch.LevelTick += Updater.OnLevelTick;
			}

            try
			{
				System.Windows.Forms.Application.ApplicationExit += delegate (object sender, EventArgs e)
				{
					if (EditorManager.inst && EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading)
					{
						string str = RTFile.BasePath;
						string modBackup = str + "level-quit-backup.lsb";
						if (RTFile.FileExists(modBackup))
							File.Delete(modBackup);

						StartCoroutine(DataManager.inst.SaveData(modBackup));
					}
				};

				Application.quitting += delegate ()
				{
					if (EditorManager.inst && EditorManager.inst.hasLoadedLevel && !EditorManager.inst.loading)
					{
						string str = RTFile.BasePath;
						string modBackup = str + "level-quit-unity-backup.lsb";
						if (RTFile.FileExists(modBackup))
							File.Delete(modBackup);

						StartCoroutine(DataManager.inst.SaveData(modBackup));
					}
				};
			}
            catch (Exception ex)
            {
				CoreHelper.LogError($"On Exit mehtods failed to set.{ex}");
            }

			Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
		}

		void Update()
        {
			EditorThemeManager.Update();

			if (Input.GetKeyDown(EventsConfig.Instance.EditorCamToggle.Value) && !LSHelpers.IsUsingInputField())
				EventsConfig.Instance.EditorCamEnabled.Value = !EventsConfig.Instance.EditorCamEnabled.Value;

			if (Input.GetKeyDown(EventsConfig.Instance.ShowGUIToggle.Value) && !LSHelpers.IsUsingInputField())
				EventsConfig.Instance.ShowGUI.Value = !EventsConfig.Instance.ShowGUI.Value;

			Application.runInBackground = CoreConfig.Instance.RunInBackground.Value;

			if (!LSHelpers.IsUsingInputField())
			{
				if (Input.GetKeyDown(CoreConfig.Instance.OpenPAFolder.Value))
					RTFile.OpenInFileBrowser.Open(RTFile.ApplicationDirectory);

				if (Input.GetKeyDown(CoreConfig.Instance.OpenPAPersistentFolder.Value))
					RTFile.OpenInFileBrowser.Open(RTFile.PersistentApplicationDirectory);

				if (Input.GetKeyDown(CoreConfig.Instance.DebugInfoToggleKey.Value))
					CoreConfig.Instance.DebugInfo.Value = !CoreConfig.Instance.DebugInfo.Value;
			}

			RTDebugger.Update();
		}

		#region Profile

		public static void SaveProfile()
		{
			var jn = JSON.Parse("{}");

			jn["user_data"]["name"] = player.sprName;
			jn["user_data"]["spr-id"] = player.sprID;

			if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "profile"))
				Directory.CreateDirectory(RTFile.ApplicationDirectory + "profile");
			RTFile.WriteToFile("profile/profile.sep", jn.ToString(3));
		}

		public static void ParseProfile()
		{
			if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + "profile"))
			{
				string rawProfileJSON = RTFile.ReadFromFile(RTFile.ApplicationDirectory + "profile/profile.sep");

				if (!string.IsNullOrEmpty(rawProfileJSON))
				{
					var jn = JSON.Parse(rawProfileJSON);

					if (!string.IsNullOrEmpty(jn["user_data"]["name"]))
					{
						player.sprName = jn["user_data"]["name"];
					}

					if (!string.IsNullOrEmpty(jn["user_data"]["spr-id"]))
					{
						player.sprID = jn["user_data"]["spr-id"];
					}
				}
			}
		}

		public static List<Universe> universes = new List<Universe>
		{
			new Universe("Axiom Nexus", Universe.UniDes.Chardax, "000"),
		};

		public static User player = new User("Player", UnityEngine.Random.Range(0, ulong.MaxValue).ToString(), new Universe(Universe.UniDes.MUS));

		public class User
		{
			public User(string _sprName, string _sprID, Universe _universe)
			{
				sprName = _sprName;
				sprID = _sprID;
				universe = _universe;
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
