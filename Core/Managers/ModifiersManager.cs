using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BasePrefab = DataManager.GameData.Prefab;

namespace BetterLegacy.Core.Managers
{
    public class ModifiersManager : MonoBehaviour
    {
        static System.Diagnostics.Stopwatch sw;

        public const string SOUNDLIBRARY_PATH = "beatmaps/soundlibrary";

        public static ModifiersManager inst;

        /// <summary>
        /// Updates modifiers on level tick.
        /// </summary>
        public static void OnLevelTick()
        {
            if (!GameData.IsValid || !CoreHelper.Playing)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1))
                sw = CoreHelper.StartNewStopwatch();

            var ldm = CoreConfig.Instance.LDM.Value;
            var beatmapObjects = GameData.Current.beatmapObjects;

            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var beatmapObject = beatmapObjects[i];

                if (beatmapObject.modifiers.Count <= 0 || ldm && beatmapObject.LDM)
                    continue;

                if (beatmapObject.orderModifiers)
                {
                    ModifiersHelper.RunModifiersLoop(beatmapObject.modifiers, beatmapObject.ignoreLifespan || beatmapObject.Alive);
                    continue;
                }

                ModifiersHelper.RunModifiersAll(beatmapObject.modifiers, beatmapObject.ignoreLifespan || beatmapObject.Alive);
            }

            foreach (var audioSource in audioSources)
            {
                try
                {
                    if (GameData.Current.beatmapObjects.Find(x => x.id == audioSource.Key) == null || !GameData.Current.beatmapObjects.Find(x => x.id == audioSource.Key).Alive)
                        queuedAudioToDelete.Add(audioSource);
                }
                catch
                {

                }
            }

            if (queuedAudioToDelete.Count > 0)
            {
                foreach (var audio in queuedAudioToDelete)
                    DeleteKey(audio.Key, audio.Value);
                queuedAudioToDelete.Clear();
            }

            if (sw != null)
            {
                CoreHelper.StopAndLogStopwatch(sw, "ModifiersManager");
                sw = null;
            }
        }

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

            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, RTFile.BepInExAssetsPath, "default_modifiers.lsb");

            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["modifiers"].Count; i++)
                defaultBeatmapObjectModifiers.Add(Modifier<BeatmapObject>.Parse(jn["modifiers"][i]));

            if (ModifiersHelper.development)
                AddDevelopmentModifiers();
        }

        void AddDevelopmentModifiers()
        {
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string> { "loadSceneDEVONLY", "False" },
                value = "Interface"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "loadStoryLevelDEVONLY",
                    "0", // chaoter
                    "0", // level
                    "False", // skip cutscenes
                },
                value = "False" // bonus chapter
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "storySaveIntVariableDEVONLY",
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "storySaveIntDEVONLY",
                    "0",
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "storySaveBoolDEVONLY",
                    "True",
                },
                value = "BoolVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "storySaveBoolDEVONLY",
                    "True",
                },
                value = "BoolVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "storyLoadIntEqualsDEVONLY",
                    "0", // Default
                    "0", // Equals
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "storyLoadIntLesserEqualsDEVONLY",
                    "0", // Default
                    "0", // Equals
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "storyLoadIntGreaterEqualsDEVONLY",
                    "0", // Default
                    "0", // Equals
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "storyLoadIntLesserDEVONLY",
                    "0", // Default
                    "0", // Equals
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "storyLoadIntGreaterDEVONLY",
                    "0", // Default
                    "0", // Equals
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "storyLoadBoolDEVONLY",
                    "False", // Default
                },
                value = "BoolVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "exampleEnableDEVONLY"
                },
                value = "False"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "exampleSayDEVONLY"
                },
                value = "Something!"
            });
        }

        public static void DeleteKey(string id, AudioSource audioSource)
        {
            Destroy(audioSource);
            audioSources.Remove(id);
        }

        public static Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

        static Modifier<T> RegisterModifier<T>(ModifierBase.Type type, string name, bool constant, params string[] values)
        {
            var modifier = new Modifier<T>
            {
                type = type,
                constant = constant,
                commands = new List<string> { name },
                value = values == null || values.Length < 1 ? string.Empty : values[0],
            };
            for (int i = 1; i < values.Length; i++)
                modifier.commands.Add(values[i]);
            return modifier;
        }

        public static List<Modifier<BeatmapObject>> defaultBeatmapObjectModifiers = new List<Modifier<BeatmapObject>>();

        public static List<Modifier<BackgroundObject>> defaultBackgroundObjectModifiers = new List<Modifier<BackgroundObject>>
        {
            RegisterModifier<BackgroundObject>(ModifierBase.Type.Action, "setActive", true, "False"),
            RegisterModifier<BackgroundObject>(ModifierBase.Type.Action, "setActiveOther", true, "False", "BG Group"),
            RegisterModifier<BackgroundObject>(ModifierBase.Type.Action, "animateObject", true, "1", "0", "0", "0", "0", "True", "0"),
            RegisterModifier<BackgroundObject>(ModifierBase.Type.Action, "animateObjectOther", true, "1", "0", "0", "0", "0", "True", "0", "BG Group"),
            RegisterModifier<BackgroundObject>(ModifierBase.Type.Action, "copyAxis", true, "Object Group", "0", "0", "0", "0", "0", "1", "0", "-99999", "99999", "99999"),
            RegisterModifier<BackgroundObject>(ModifierBase.Type.Trigger, "timeLesserEquals", true, "0"),
            RegisterModifier<BackgroundObject>(ModifierBase.Type.Trigger, "timeGreaterEquals", true, "0"),
            RegisterModifier<BackgroundObject>(ModifierBase.Type.Trigger, "timeLesser", true, "0"),
            RegisterModifier<BackgroundObject>(ModifierBase.Type.Trigger, "timeGreater", true, "0"),
        };

        public static List<Modifier<CustomPlayer>> defaultPlayerModifiers = new List<Modifier<CustomPlayer>>
        {
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Action, "setCustomActive", true, "False", "0", "True"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Action, "kill", false, ""),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Action, "hit", false, ""),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Action, "boost", false, ""),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Action, "shoot", false, ""),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Action, "pulse", false, ""),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Action, "jump", false, ""),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Action, "signalModifier", false, "0", "Object Group"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Action, "playAnimation", false, "0", "boost"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Action, "setIdleAnimation", false, "0", "boost"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Action, "animateObject", true, "1", "0", "0", "0", "0", "True", "0", "0"),

            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "keyPressDown", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "keyPress", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "keyPressUp", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "mouseButtonDown", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "mouseButton", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "mouseButtonUp", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "controlPressDown", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "controlPress", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "controlPressUp", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "healthEquals", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "healthGreaterEquals", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "healthLesserEquals", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "healthGreater", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "healthLesser", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "healthPerEquals", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "healthPerGreaterEquals", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "healthPerLesserEquals", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "healthPerGreater", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "healthPerLesser", true, "0"),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "isDead", true, ""),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "isBoosting", true, ""),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "isColliding", true, ""),
            RegisterModifier<CustomPlayer>(ModifierBase.Type.Trigger, "isSolidColliding", true, ""),
        };
    }
}
