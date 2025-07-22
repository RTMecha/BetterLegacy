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

            modifiers.ForLoop(modifier =>
            {
                if (modifier.Name.Contains("DEVONLY"))
                    modifier.hideInEditor = !ModifiersHelper.development;
            });
        }

        void Awake()
        {
            inst = this;
            modifiers.Clear();

            LoadFile(modifiers, RTFile.CombinePaths(RTFile.ApplicationDirectory, RTFile.BepInExAssetsPath, "default_modifiers.json"));

            AddDevelopmentModifiers();

            modifiers.AddRange(defaultPlayerModifiers);
        }

        void LoadFile(List<Modifier> modifiers, string path)
        {
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["modifiers"].Count; i++)
                LoadModifiers(modifiers, jn["modifiers"][i]);
        }

        void LoadModifiers(List<Modifier> modifiers, JSONNode jn)
        {
            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                    LoadModifiers(modifiers, jn[i]);
                return;
            }

            var modifier = Modifier.Parse(jn);
            modifier.compatibility = ModifierCompatibility.Parse(jn["compat"]);
            modifiers.Add(modifier);
        }

        void AddDevelopmentModifiers()
        {
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "loadSceneDEVONLY", false, "Interface", "False"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "loadStoryLevelDEVONLY", false, "False", "0", "0", "False", "0"));

            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "storySaveBoolDEVONLY", false, "BoolVariable", "True"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "storySaveIntDEVONLY", false, "IntVariable", "0"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "storySaveFloatDEVONLY", false, "FloatVariable", "0"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "storySaveStringDEVONLY", false, "StringVariable", "Value"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "storySaveIntVariableDEVONLY", false, "IntVariable"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "getStorySaveBoolDEVONLY", true, "STORY_BOOL_VAR", "BoolVariable", "False"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "getStorySaveIntDEVONLY", true, "STORY_INT_VAR", "IntVariable", "0"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "getStorySaveFloatDEVONLY", true, "STORY_FLOAT_VAR", "FloatVariable", "0"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "getStorySaveStringDEVONLY", true, "STORY_STRING_VAR", "StringVariable", string.Empty));

            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "exampleEnableDEVONLY", false, "False"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Action, "exampleSayDEVONLY", false, "Something!"));

            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Trigger, "storyLoadIntEqualsDEVONLY", false, "IntVariable", "0", "0"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Trigger, "storyLoadIntLesserEqualsDEVONLY", false, "IntVariable", "0", "0"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Trigger, "storyLoadIntGreaterEqualsDEVONLY", false, "IntVariable", "0", "0"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Trigger, "storyLoadIntLesserDEVONLY", false, "IntVariable", "0", "0"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Trigger, "storyLoadIntGreaterDEVONLY", false, "IntVariable", "0", "0"));
            modifiers.Add(RegisterModifierDEVONLY(Modifier.Type.Trigger, "storyLoadBoolDEVONLY", false, "BoolVariable", "False"));
        }

        public static void DeleteKey(string id, AudioSource audioSource)
        {
            Destroy(audioSource);
            audioSources.Remove(id);
        }

        public static Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

        static Modifier RegisterModifier(Modifier.Type type, string name, bool constant, params string[] values) => new Modifier(type, name, constant, values);
        
        static Modifier RegisterModifierDEVONLY(Modifier.Type type, string name, bool constant, params string[] values) => new Modifier(type, name, constant, values) { hideInEditor = true };

        public List<Modifier> modifiers = new List<Modifier>();

        public static List<Modifier> defaultLevelModifiers = new List<Modifier>()
        {
            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Action, "playerBubble", false,
                "Text", // Text
                "0" // Time
                ),
            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Action, "playerMoveAll", false,
                "0", // X
                "0", // Y
                "0" // Time
                ),
            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Action, "playerEnableBoostAll", false,
                "False", // Lock Enabled
                "", // Show Bubble
                "" // Bubble Time
                ),
            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Action, "playerXLock", false,
                "False", // Lock Enabled
                "", // Show Bubble
                "" // Lock Enabled
                ),
            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Action, "playerYLock", false,
                "False", // Lock Enabled
                "", // Show Bubble
                "" // Lock Enabled
                ),
            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Action, "setMusicTime", false,
                "0" // Time
                ),
            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Action, "setPitch", false,
                "1" // Time
                ),

            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Trigger, "timeInRange", false,
                "", // IDK
                "0", // Activation Time Range Min
                "0" // Activation Time Range Max
                ),
            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Trigger, "playerHit", false,
                "", // IDK
                "0", // Activation Time Range Min
                "0" // Activation Time Range Max
                ),
            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Trigger, "playerDeath", false,
                "", // IDK
                "0", // Activation Time Range Min
                "0" // Activation Time Range Max
                ),
            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Trigger, "onLevelStart", false,
                "", // IDK
                "0", // Activation Time Range Min
                "0" // Activation Time Range Max
                ),
            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Trigger, "onLevelRestart", false,
                "", // IDK
                "0", // Activation Time Range Min
                "0" // Activation Time Range Max
                ),
            new Modifier(ModifierCompatibility.GameDataCompatible, Modifier.Type.Trigger, "onLevelRewind", false,
                "", // IDK
                "0", // Activation Time Range Min
                "0" // Activation Time Range Max
                ),
        };

        public static List<Modifier> defaultPlayerModifiers = new List<Modifier>
        {
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, "setCustomActive", true, "False", "0", "True"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, "kill", false, ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, "hit", false, ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, "boost", false, ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, "shoot", false, ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, "pulse", false, ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, "jump", false, ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, "signalModifier", false, "0", "Object Group"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, "playAnimation", false, "0", "boost"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, "setIdleAnimation", false, "0", "boost"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, "animateObject", true, "1", "0", "0", "0", "0", "True", "0", "0"),

            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "keyPressDown", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "keyPress", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "keyPressUp", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "mouseButtonDown", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "mouseButton", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "mouseButtonUp", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "controlPress", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "controlPressUp", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "healthEquals", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "healthGreaterEquals", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "healthLesserEquals", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "healthGreater", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "healthLesser", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "healthPerEquals", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "healthPerGreaterEquals", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "healthPerLesserEquals", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "healthPerGreater", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "healthPerLesser", true, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "isDead", true, ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "isBoosting", true, ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "isColliding", true, ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Trigger, "isSolidColliding", true, ""),
        };
    }
}
