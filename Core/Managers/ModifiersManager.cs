using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Data.Modifiers;
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

        void Awake()
        {
            inst = this;
            modifiers.Clear();

            LoadFile(modifiers, RTFile.CombinePaths(RTFile.ApplicationDirectory, RTFile.BepInExAssetsPath, "default_modifiers.json"));

            AddDevelopmentModifiers();

            modifiers.AddRange(defaultPlayerModifiers);
            modifiers.ForLoop(modifier =>
            {
                var name = modifier.Name;

                if (modifier.type == Modifier.Type.Trigger && ModifiersHelper.triggers.TryFind(x => x.name == name, out ModifierTrigger trigger))
                    modifier.Trigger = trigger.function;

                if (modifier.type == Modifier.Type.Action && ModifiersHelper.actions.TryFind(x => x.name == name, out ModifierAction action))
                    modifier.Action = action.function;

                if (ModifiersHelper.inactives.TryFind(x => x.name == name, out ModifierInactive inactive))
                    modifier.Inactive = inactive.function;
            });
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
            modifiers.Add(new Modifier(ModifierCompatibility.LevelControlCompatible.WithStoryOnly(), Modifier.Type.Action, "loadSceneDEVONLY", false, "Interface", "False"));
            modifiers.Add(new Modifier(ModifierCompatibility.LevelControlCompatible.WithStoryOnly(), Modifier.Type.Action, "loadStoryLevelDEVONLY", false, "False", "0", "0", "False", "0"));

            modifiers.Add(new Modifier(ModifierCompatibility.LevelControlCompatible.WithStoryOnly(), Modifier.Type.Action, "storySaveBoolDEVONLY", false, "BoolVariable", "True"));
            modifiers.Add(new Modifier(ModifierCompatibility.LevelControlCompatible.WithStoryOnly(), Modifier.Type.Action, "storySaveIntDEVONLY", false, "IntVariable", "0"));
            modifiers.Add(new Modifier(ModifierCompatibility.LevelControlCompatible.WithStoryOnly(), Modifier.Type.Action, "storySaveFloatDEVONLY", false, "FloatVariable", "0"));
            modifiers.Add(new Modifier(ModifierCompatibility.LevelControlCompatible.WithStoryOnly(), Modifier.Type.Action, "storySaveStringDEVONLY", false, "StringVariable", "Value"));
            modifiers.Add(new Modifier(ModifierCompatibility.LevelControlCompatible.WithStoryOnly(), Modifier.Type.Action, "storySaveIntVariableDEVONLY", false, "IntVariable"));
            modifiers.Add(new Modifier(ModifierCompatibility.AllCompatible.WithStoryOnly(), Modifier.Type.Action, "getStorySaveBoolDEVONLY", true, "STORY_BOOL_VAR", "BoolVariable", "False"));
            modifiers.Add(new Modifier(ModifierCompatibility.AllCompatible.WithStoryOnly(), Modifier.Type.Action, "getStorySaveIntDEVONLY", true, "STORY_INT_VAR", "IntVariable", "0"));
            modifiers.Add(new Modifier(ModifierCompatibility.AllCompatible.WithStoryOnly(), Modifier.Type.Action, "getStorySaveFloatDEVONLY", true, "STORY_FLOAT_VAR", "FloatVariable", "0"));
            modifiers.Add(new Modifier(ModifierCompatibility.AllCompatible.WithStoryOnly(), Modifier.Type.Action, "getStorySaveStringDEVONLY", true, "STORY_STRING_VAR", "StringVariable", string.Empty));

            modifiers.Add(new Modifier(ModifierCompatibility.LevelControlCompatible.WithStoryOnly(), Modifier.Type.Action, "exampleEnableDEVONLY", false, "False"));
            modifiers.Add(new Modifier(ModifierCompatibility.LevelControlCompatible.WithStoryOnly(), Modifier.Type.Action, "exampleSayDEVONLY", false, "Something!"));

            modifiers.Add(new Modifier(ModifierCompatibility.AllCompatible.WithStoryOnly(), Modifier.Type.Trigger, "storyLoadIntEqualsDEVONLY", false, "IntVariable", "0", "0"));
            modifiers.Add(new Modifier(ModifierCompatibility.AllCompatible.WithStoryOnly(), Modifier.Type.Trigger, "storyLoadIntLesserEqualsDEVONLY", false, "IntVariable", "0", "0"));
            modifiers.Add(new Modifier(ModifierCompatibility.AllCompatible.WithStoryOnly(), Modifier.Type.Trigger, "storyLoadIntGreaterEqualsDEVONLY", false, "IntVariable", "0", "0"));
            modifiers.Add(new Modifier(ModifierCompatibility.AllCompatible.WithStoryOnly(), Modifier.Type.Trigger, "storyLoadIntLesserDEVONLY", false, "IntVariable", "0", "0"));
            modifiers.Add(new Modifier(ModifierCompatibility.AllCompatible.WithStoryOnly(), Modifier.Type.Trigger, "storyLoadIntGreaterDEVONLY", false, "IntVariable", "0", "0"));
            modifiers.Add(new Modifier(ModifierCompatibility.AllCompatible.WithStoryOnly(), Modifier.Type.Trigger, "storyLoadBoolDEVONLY", false, "BoolVariable", "False"));
        }

        public static void DeleteKey(string id, AudioSource audioSource)
        {
            Destroy(audioSource);
            audioSources.Remove(id);
        }

        public static Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

        static Modifier RegisterModifier(Modifier.Type type, string name, bool constant, params string[] values) => new Modifier(type, name, constant, values);
        
        static Modifier RegisterModifierDEVONLY(Modifier.Type type, string name, bool constant, params string[] values) => new Modifier(ModifierCompatibility.LevelControlCompatible.WithStoryOnly(), type, name, constant, values);

        public List<Modifier> modifiers = new List<Modifier>();

        public List<Modifier> defaultLevelModifiers = new List<Modifier>()
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

        public List<Modifier> defaultPlayerModifiers = new List<Modifier>
        {
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.setCustomObjectActive), true, "False", "0", "True"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.setCustomObjectIdle), true, "0", "True"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.playAnimation), false, "0", "boost"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.setIdleAnimation), false, "0", "boost"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.kill), false, ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.hit), false, "0"),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.boost), false, "", ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.shoot), false, ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.pulse), false, ""),
            new Modifier(ModifierCompatibility.PAPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.jump), false, ""),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.getHealth), true, "HEALTH_VAR"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.getLives), true, "LIVES_VAR"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.getMaxHealth), true, "MAX_HEALTH_VAR"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.getMaxLives), true, "MAX_LIVES_VAR"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.getIndex), true, "INDEX_VAR"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.getMove), true, "MOVE_X_VAR", "MOVE_Y_VAR", "True"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.getMoveX), true, "MOVE_X_VAR", "True"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Action, nameof(ModifierActions.getMoveY), true, "MOVE_Y_VAR", "True"),

            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Trigger, nameof(ModifierTriggers.healthEquals), true, "0"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Trigger, nameof(ModifierTriggers.healthGreaterEquals), true, "0"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Trigger, nameof(ModifierTriggers.healthLesserEquals), true, "0"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Trigger, nameof(ModifierTriggers.healthGreater), true, "0"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Trigger, nameof(ModifierTriggers.healthLesser), true, "0"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Trigger, nameof(ModifierTriggers.isDead), true, "0"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Trigger, nameof(ModifierTriggers.isBoosting), true, "0"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Trigger, nameof(ModifierTriggers.isColliding), true, "0"),
            new Modifier(ModifierCompatibility.FullPlayerCompatible, Modifier.Type.Trigger, nameof(ModifierTriggers.isSolidColliding), true, "0"),
        };
    }
}
