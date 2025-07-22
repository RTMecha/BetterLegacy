using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Managers
{
    public class ModifiersManager : MonoBehaviour
    {
        public const string SOUNDLIBRARY_PATH = "beatmaps/soundlibrary";

        public static ModifiersManager inst;

        public static List<KeyValuePair<string, AudioSource>> queuedAudioToDelete = new List<KeyValuePair<string, AudioSource>>();

        /// <summary>
        /// Inits ModifiersManager.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(ModifiersManager), SystemManager.inst.transform).AddComponent<ModifiersManager>();

        public void ToggleDevelopment()
        {
            ModifiersHelper.development = !ModifiersHelper.development;

            if (!ModifiersHelper.development)
                defaultBeatmapObjectModifiers.RemoveAll(x => x.Name.Contains("DEVONLY"));
            else
                AddDevelopmentModifiers();
        }

        void Awake()
        {
            inst = this;
            defaultBeatmapObjectModifiers.Clear();
            defaultBackgroundObjectModifiers.Clear();

            LoadFile<BeatmapObject>(defaultBeatmapObjectModifiers, RTFile.CombinePaths(RTFile.ApplicationDirectory, RTFile.BepInExAssetsPath, "default_modifiers.lsb"));
            LoadFile<BackgroundObject>(defaultBackgroundObjectModifiers, RTFile.CombinePaths(RTFile.ApplicationDirectory, RTFile.BepInExAssetsPath, "default_bg_modifiers.lsb"));

            if (ModifiersHelper.development)
                AddDevelopmentModifiers();
        }

        void LoadFile<T>(List<ModifierBase> modifiers, string path)
        {
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["modifiers"].Count; i++)
                LoadModifiers<T>(modifiers, jn["modifiers"][i]);
        }

        void LoadModifiers<T>(List<ModifierBase> modifiers, JSONNode jn)
        {
            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                    LoadModifiers<T>(modifiers, jn[i]);
                return;
            }

            modifiers.Add(Modifier<T>.Parse(jn));
        }

        void AddDevelopmentModifiers()
        {
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "loadSceneDEVONLY", false, "Interface", "False"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "loadStoryLevelDEVONLY", false, "False", "0", "0", "False", "0"));

            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "storySaveBoolDEVONLY", false, "BoolVariable", "True"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "storySaveIntDEVONLY", false, "IntVariable", "0"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "storySaveFloatDEVONLY", false, "FloatVariable", "0"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "storySaveStringDEVONLY", false, "StringVariable", "Value"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "storySaveIntVariableDEVONLY", false, "IntVariable"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "getStorySaveBoolDEVONLY", true, "STORY_BOOL_VAR", "BoolVariable", "False"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "getStorySaveIntDEVONLY", true, "STORY_INT_VAR", "IntVariable", "0"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "getStorySaveFloatDEVONLY", true, "STORY_FLOAT_VAR", "FloatVariable", "0"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "getStorySaveStringDEVONLY", true, "STORY_STRING_VAR", "StringVariable", string.Empty));

            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "exampleEnableDEVONLY", false, "False"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Action, "exampleSayDEVONLY", false, "Something!"));

            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Trigger, "storyLoadIntEqualsDEVONLY", false, "IntVariable", "0", "0"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Trigger, "storyLoadIntLesserEqualsDEVONLY", false, "IntVariable", "0", "0"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Trigger, "storyLoadIntGreaterEqualsDEVONLY", false, "IntVariable", "0", "0"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Trigger, "storyLoadIntLesserDEVONLY", false, "IntVariable", "0", "0"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Trigger, "storyLoadIntGreaterDEVONLY", false, "IntVariable", "0", "0"));
            defaultBeatmapObjectModifiers.Add(RegisterModifier<BeatmapObject>(ModifierBase.Type.Trigger, "storyLoadBoolDEVONLY", false, "BoolVariable", "False"));
        }

        public static void DeleteKey(string id, AudioSource audioSource)
        {
            Destroy(audioSource);
            audioSources.Remove(id);
        }

        public static Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

        static Modifier<T> RegisterModifier<T>(ModifierBase.Type type, string name, bool constant, params string[] values)
        {
            var modifier = new Modifier<T>(name)
            {
                type = type,
                constant = constant,
                value = values == null || values.IsEmpty() ? string.Empty : values[0],
            };
            for (int i = 1; i < values.Length; i++)
                modifier.commands.Add(values[i]);
            return modifier;
        }

        public static List<ModifierBase> defaultLevelModifiers = new List<ModifierBase>()
        {
            new Modifier<GameData>("playerBubble")
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerBubble",
					"0", // Time
                },
                value = "Text", // Text
            }, // playerBubble (todo)
			new Modifier<GameData>("playerMoveAll")
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerLocation",
					"0", // Y
					"0", // Time
                },
                value = "0", // X
            }, // playerLocation
			new Modifier<GameData>("playerEnableBoostAll")
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerBoostLock",
					"", // Show Bubble
					"", // Bubble Time
                },
                value = "False", // Lock Enabled
            }, // playerEnableBoostAll
			new Modifier<GameData>("playerXLock")
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerXLock",
					"", // Show Bubble
					"", // Bubble Time
                },
                value = "False", // Lock Enabled
            }, // playerXLock
			new Modifier<GameData>("playerYLock")
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerYLock",
                    "", // Show Bubble
					"", // Bubble Time
                },
                value = "False", // Lock Enabled
            }, // playerYLock
			new Modifier<GameData>("playerBoost")
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerBoost",
                    "0", // Y
                },
                value = "0", // X
            }, // playerBoost
			new Modifier<GameData>("setMusicTime")
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "setMusicTime"
                },
                value = "0", // time
            }, // setMusicTime
			new Modifier<GameData>("setPitch")
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "setPitch"
                },
                value = "0", // time
            }, // setPitch

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
			new Modifier<GameData>("levelRestart")
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "levelRestart",
                    "0", // Activation Time Range Min
					"0", // Activation Time Range Max
				},
                value = "",
            }, // levelRestart
			new Modifier<GameData>("levelRewind")
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "levelRewind",
                    "0", // Activation Time Range Min
					"0", // Activation Time Range Max
				},
                value = "",
            }, // levelRewind
        };

        public static List<ModifierBase> defaultBeatmapObjectModifiers = new List<ModifierBase>();

        public static List<ModifierBase> defaultBackgroundObjectModifiers = new List<ModifierBase>();
        
        public static List<ModifierBase> defaultPrefabObjectModifiers = new List<ModifierBase>();

        public static List<ModifierBase> defaultPlayerModifiers = new List<ModifierBase>
        {
            RegisterModifier<PAPlayer>(ModifierBase.Type.Action, "setCustomActive", true, "False", "0", "True"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Action, "kill", false, ""),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Action, "hit", false, ""),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Action, "boost", false, ""),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Action, "shoot", false, ""),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Action, "pulse", false, ""),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Action, "jump", false, ""),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Action, "signalModifier", false, "0", "Object Group"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Action, "playAnimation", false, "0", "boost"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Action, "setIdleAnimation", false, "0", "boost"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Action, "animateObject", true, "1", "0", "0", "0", "0", "True", "0", "0"),

            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "keyPressDown", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "keyPress", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "keyPressUp", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "mouseButtonDown", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "mouseButton", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "mouseButtonUp", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "controlPress", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "controlPressUp", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "healthEquals", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "healthGreaterEquals", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "healthLesserEquals", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "healthGreater", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "healthLesser", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "healthPerEquals", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "healthPerGreaterEquals", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "healthPerLesserEquals", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "healthPerGreater", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "healthPerLesser", true, "0"),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "isDead", true, ""),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "isBoosting", true, ""),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "isColliding", true, ""),
            RegisterModifier<PAPlayer>(ModifierBase.Type.Trigger, "isSolidColliding", true, ""),
        };
    }
}
