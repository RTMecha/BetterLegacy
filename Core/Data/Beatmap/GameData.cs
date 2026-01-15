using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents a Project Arrhythmia level.
    /// </summary>
    public class GameData : PAObject<GameData>, IModifyable, IModifierReference, IBeatmap, IFile
    {
        public GameData() { }

        #region Values

        /// <summary>
        /// The current GameData that is being used by the game.
        /// </summary>
        public static GameData Current { get; set; }

        #region Verifying

        public bool Modded => BeatmapObjectsModded || EventKeyframesModded || PrefabObjectsModded;

        bool BeatmapObjectsModded => beatmapObjects.Any(x =>
            x.modifiers.Count > 0 ||
            x.objectType == BeatmapObject.ObjectType.Solid ||
            x.desync ||
            x.renderLayerType != BeatmapObject.RenderLayerType.Foreground ||
            x.detailMode != DetailMode.Normal ||
            x.colorBlendMode != ColorBlendMode.Normal ||
            x.parallaxSettings.Any(y => y != 1f) ||
            x.parentAdditive != "000" ||
            x.shape > UnmoddedShapeOptions.Length - 1 ||
            x.shapeOption >= UnmoddedShapeOptions[Mathf.Clamp(x.shape, 0, UnmoddedShapeOptions.Length - 1)] ||
            x.events[0].Any(x => x.random > 4 || x.values.Length > 2 && x.values[2] != 0f || x.relative) ||
            x.events[1].Any(x => x.relative) ||
            x.events[2].Any(x => x.random > 4 || !x.relative) ||
            x.events[3].Any(x => x.random > 4 || x.values[0] > 8f || x.values[2] != 0f || x.values[3] != 0f || x.values[4] != 0f));

        bool EventKeyframesModded
        {
            get
            {
                bool eventKeyframesModded = false;

                for (int i = 0; i < events.Count; i++)
                {
                    for (int j = 0; j < events[i].Count; j++)
                    {
                        var eventKeyframe = events[i][j];

                        for (int k = 0; k < eventKeyframe.values.Length; k++)
                        {
                            if ((EventLibrary.vanillaLegacyValueCounts.Length <= i || EventLibrary.vanillaLegacyValueCounts[i] <= k) && EventLibrary.cachedDefaultKeyframes[i].values[k] != eventKeyframe.values[k])
                            {
                                eventKeyframesModded = true;
                                break;
                            }
                        }

                        if (eventKeyframesModded)
                            break;
                    }

                    if (eventKeyframesModded)
                        break;
                }

                return eventKeyframesModded;
            }
        }

        bool PrefabObjectsModded => prefabObjects.Any(x =>
            x.RepeatCount > 0 ||
            x.Speed != 1f ||
            !string.IsNullOrEmpty(x.Parent) ||
            x.autoKillType != PrefabAutoKillType.Regular ||
            x.depth != 0f ||
            x.desync ||
            x.parentParallax.Any(y => y != 1f) ||
            x.parentAdditive != "000");

        #endregion

        /// <summary>
        /// If opacity should be saved to themes.
        /// </summary>
        public static bool SaveOpacityToThemes { get; set; } = false;

        /// <summary>
        /// The vanilla shape options.
        /// </summary>
        public static int[] UnmoddedShapeOptions => new int[]
        {
            3,
            9,
            4,
            2,
            1,
            6
        };

        // debug
        //public static JSONNode LastParsedJSON { get; set; }
        //public static GameData ConvertedGameData { get; set; }

        public List<string> Tags { get; set; }

        public List<Modifier> Modifiers { get => modifiers; set => modifiers = value; }

        public bool IgnoreLifespan { get; set; }

        public bool OrderModifiers { get; set; }

        public bool ModifiersActive => true;

        public ModifierReferenceType ReferenceType => ModifierReferenceType.GameData;

        public int IntVariable { get; set; }

        public BeatmapData data;

        public Assets assets = new Assets();

        public List<BeatmapObject> BeatmapObjects { get => beatmapObjects; set => beatmapObjects = value; }
        public List<BeatmapObject> beatmapObjects = new List<BeatmapObject>();

        public List<PrefabObject> PrefabObjects { get => prefabObjects; set => prefabObjects = value; }
        public List<PrefabObject> prefabObjects = new List<PrefabObject>();

        public List<Prefab> Prefabs { get => prefabs; set => prefabs = value; }
        public List<Prefab> prefabs = new List<Prefab>();

        public int mainBackgroundLayer;

        public List<BackgroundLayer> BackgroundLayers { get => backgroundLayers; set => backgroundLayers = value; }
        public List<BackgroundLayer> backgroundLayers = new List<BackgroundLayer>();

        public List<BackgroundObject> BackgroundObjects { get => backgroundObjects; set => backgroundObjects = value; }
        public List<BackgroundObject> backgroundObjects = new List<BackgroundObject>();

        public List<Modifier> modifiers = new List<Modifier>();

        public List<ModifierBlock> modifierBlocks = new List<ModifierBlock>();

        public List<BeatmapTheme> BeatmapThemes { get => beatmapThemes; set => beatmapThemes = value; }
        public List<BeatmapTheme> beatmapThemes = new List<BeatmapTheme>();

        public List<List<EventKeyframe>> events = new List<List<EventKeyframe>>();

        public List<PAAnimation> animations = new List<PAAnimation>();

        public List<BeatmapVariable> variables = new List<BeatmapVariable>();

        public RTLevelBase ParentRuntime { get; set; }

        public enum DuplicateBehaviorType
        {
            Remove,
            DontAdd,
            Keep,
        }
        public static DuplicateBehaviorType DuplicateBehavior { get; set; } = DuplicateBehaviorType.Remove;

        #endregion

        #region Functions

        public override void CopyData(GameData orig, bool newID = true)
        {
            if (!orig)
                return;

            data = orig.data?.Copy();
            beatmapObjects = new List<BeatmapObject>(orig.beatmapObjects.Select(x => x.Copy(false)));
            backgroundLayers = new List<BackgroundLayer>(orig.backgroundLayers.Select(x => x.Copy(false)));
            backgroundObjects = new List<BackgroundObject>(orig.backgroundObjects.Select(x => x.Copy(false)));
            prefabObjects = new List<PrefabObject>(orig.prefabObjects.Select(x => x.Copy(false)));
            prefabs = new List<Prefab>(orig.prefabs.Select(x => x.Copy(false)));
            beatmapThemes = new List<BeatmapTheme>(orig.beatmapThemes.Select(x => x.Copy(false)));
            animations = new List<PAAnimation>(orig.animations.Select(x => x.Copy(false)));
            modifiers = new List<Modifier>(orig.modifiers.Select(x => x.Copy(false)));
            modifierBlocks = new List<ModifierBlock>(orig.modifierBlocks.Select(x => x.Copy(false)));
            mainBackgroundLayer = orig.mainBackgroundLayer;
            variables = new List<BeatmapVariable>(orig.variables.Select(x => x.Copy(false)));

            events.Clear();
            for (int i = 0; i < orig.events.Count; i++)
                events.Add(orig.events[i].Select(x => x.Copy()).ToList());
        }

        /// <summary>
        /// Parses a level from a file.
        /// </summary>
        /// <param name="path">File to parse.</param>
        /// <param name="fileType">The type of Project Arrhythmia the file is from.</param>
        /// <param name="parseThemes">If the levels' themes should overwrite the current global list of themes.</param>
        /// <param name="version">The exact version the level is from.</param>
        /// <returns>Returns a parsed <see cref="GameData"/>.</returns>
        public static GameData ReadFromFile(string path, ArrhythmiaType fileType, Version version = default) => fileType switch
        {
            ArrhythmiaType.LS => Parse(JSON.Parse(RTFile.ReadFromFile(path))),
            ArrhythmiaType.VG => ParseVG(JSON.Parse(RTFile.ReadFromFile(path)), version),
            _ => null,
        };

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            var parseOptimizations = CoreConfig.Instance.ParseOptimizations.Value;

            CoreHelper.Log($"Parsing Version: {version}");

            for (int i = 0; i < jn["triggers"].Count; i++)
            {
                var jnTrigger = jn["triggers"][i];

                var eventType = jnTrigger["event_type"].AsInt;
                var eventTrigger = jnTrigger["event_trigger"].AsInt;

                var triggerModifier = new Modifier(ModifiersHelper.GetLevelTriggerName(eventTrigger));
                triggerModifier.triggerCount = jnTrigger["event_retrigger"].AsInt;
                triggerModifier.SetValue(1, jnTrigger["event_trigger_time"]["x"].AsFloat.ToString());
                triggerModifier.SetValue(2, jnTrigger["event_trigger_time"]["y"].AsFloat.ToString());

                var actionModifier = new Modifier(ModifiersHelper.GetLevelActionName(eventType));

                for (int j = 0; j < jnTrigger["event_data"].Count; j++)
                    actionModifier.SetValue(j, jnTrigger["event_data"][j]);

                modifiers.Add(triggerModifier);
                modifiers.Add(actionModifier);
            }

            CoreHelper.Log($"Parsing BeatmapData");
            data = BeatmapData.ParseVG(jn);

            CoreHelper.Log($"Parsing Objects");
            for (int i = 0; i < jn["objects"].Count; i++)
                beatmapObjects.Add(BeatmapObject.ParseVG(jn["objects"][i], version));

            if (parseOptimizations)
                for (int i = 0; i < beatmapObjects.Count; i++)
                    beatmapObjects[i].SetAutokillToScale(beatmapObjects);

            CoreHelper.Log($"Parsing Prefab Objects");
            for (int i = 0; i < jn["prefab_objects"].Count; i++)
                prefabObjects.Add(PrefabObject.ParseVG(jn["prefab_objects"][i]));

            CoreHelper.Log($"Parsing Prefabs");
            for (int i = 0; i < jn["prefabs"].Count; i++)
                prefabs.Add(Prefab.ParseVG(jn["prefabs"][i], version));

            Dictionary<string, string> idConversion = new Dictionary<string, string>();

            if (jn["themes"] != null)
            {
                CoreHelper.Log($"Parsing Beatmap Themes");

                for (int i = 0; i < jn["themes"].Count; i++)
                {
                    var beatmapTheme = BeatmapTheme.ParseVG(jn["themes"][i]);

                    if (!string.IsNullOrEmpty(beatmapTheme.VGID))
                        idConversion.TryAdd(beatmapTheme.VGID, beatmapTheme.id);

                    beatmapThemes.Add(beatmapTheme);

                    beatmapTheme = null;
                }

                ThemeManager.inst.UpdateAllThemes();
            }

            if (jn["parallax_settings"] != null)
            {
                mainBackgroundLayer = jn["parallax_settings"]["ml"].AsInt;
                // parse depth of field here when that event is added
                for (int i = 0; i < jn["parallax_settings"]["l"].Count; i++)
                {
                    var jnLayer = jn["parallax_settings"]["l"][i];
                    var backgroundLayer = BackgroundLayer.ParseVG(jnLayer, version);
                    backgroundLayers.Add(backgroundLayer);
                    for (int j = 0; j < jnLayer["o"].Count; j++)
                    {
                        var bg = BackgroundObject.ParseVG(jnLayer["o"][j], version);
                        bg.layer = backgroundLayer.id;
                        backgroundObjects.Add(bg);
                    }
                }
            }

            events = new List<List<EventKeyframe>>();

            string breakContext = string.Empty;
            try
            {
                CoreHelper.Log($"Parsing VG Event Keyframes");
                // Move
                breakContext = "Move";
                events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][0].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][0][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetValues(kfjn["ev"][0].AsFloat, kfjn["ev"][1].AsFloat);

                    events[0].Add(eventKeyframe);
                }

                // Zoom
                breakContext = "Zoom";
                events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][1].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][1][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetValues(kfjn["ev"][0].AsFloat);

                    events[1].Add(eventKeyframe);
                }

                // Rotate
                breakContext = "Rotate";
                events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][2].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][2][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetValues(kfjn["ev"][0].AsFloat);

                    events[2].Add(eventKeyframe);
                }

                // Shake
                breakContext = "Shake";
                events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][3].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][3][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;

                    if (kfjn["ev"].Count > 3)
                        eventKeyframe.SetValues(kfjn["ev"][0].AsFloat, kfjn["ev"][1].AsFloat, kfjn["ev"][2].AsFloat);
                    else
                        eventKeyframe.SetValues(kfjn["ev"][0].AsFloat, 1f, 1f);

                    events[3].Add(eventKeyframe);
                }

                // Theme
                breakContext = "Theme";
                events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][4].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][4][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    // Since theme keyframes use random string IDs as their value instead of numbers (wtf), we have to convert the new IDs to numbers.
                    if (!string.IsNullOrEmpty(kfjn["evs"][0]) && idConversion.TryGetValue(kfjn["evs"][0], out string themeID))
                        eventKeyframe.SetValues(Parser.TryParse(themeID, 0f));
                    else
                        eventKeyframe.SetValues(0f);

                    events[4].Add(eventKeyframe);
                }

                // Chroma
                breakContext = "Chroma";
                events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][5].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][5][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetValues(kfjn["ev"][0].AsFloat);

                    events[5].Add(eventKeyframe);
                }

                // Bloom
                breakContext = "Bloom";
                events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][6].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][6][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        1f,
                        0f,
                        kfjn["ev"][2].AsFloat == 9f ? 18f : kfjn["ev"][2].AsFloat);

                    events[6].Add(eventKeyframe);
                }

                // Vignette
                breakContext = "Vignette";
                events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][7].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][7][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        kfjn["ev"][2].AsFloat,
                        1f,
                        kfjn["ev"][4].AsFloat,
                        kfjn["ev"][5].AsFloat,
                        kfjn["ev"][6].AsFloat == 9f ? 18f : kfjn["ev"][6].AsFloat);

                    events[7].Add(eventKeyframe);
                }

                // Lens
                breakContext = "Lens";
                events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][8].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][8][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        kfjn["ev"][2].AsFloat,
                        1f,
                        1f,
                        1f);

                    events[8].Add(eventKeyframe);
                }

                // Grain
                breakContext = "Grain";
                events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][9].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][9][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        kfjn["ev"][2].AsFloat);

                    events[9].Add(eventKeyframe);
                }

                // Hue
                breakContext = "Hue";
                events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][12].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][12][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetValues(
                        kfjn["ev"][0].AsFloat);

                    events[10].Add(eventKeyframe);
                }

                events.Add(new List<EventKeyframe>());
                events[11].Add(EventLibrary.cachedDefaultKeyframes[11].Copy());
                events.Add(new List<EventKeyframe>());
                events[12].Add(EventLibrary.cachedDefaultKeyframes[12].Copy());
                events.Add(new List<EventKeyframe>());
                events[13].Add(EventLibrary.cachedDefaultKeyframes[13].Copy());
                events.Add(new List<EventKeyframe>());
                events[14].Add(EventLibrary.cachedDefaultKeyframes[14].Copy());

                // Gradient
                breakContext = "Gradient";
                events.Add(new List<EventKeyframe>());
                for (int i = 0; i < jn["events"][10].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][10][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat,
                        kfjn["ev"][2].AsFloat == 9f ? 19f : kfjn["ev"][2].AsFloat,
                        kfjn["ev"][3].AsFloat == 9f ? 19f : kfjn["ev"][3].AsFloat,
                        kfjn["ev"].Count > 4 ? kfjn["ev"][4].AsFloat : 0f);

                    events[15].Add(eventKeyframe);
                }

                for (int i = 16; i < EventLibrary.cachedDefaultKeyframes.Count; i++)
                {
                    events.Add(new List<EventKeyframe>());
                    events[i].Add(EventLibrary.cachedDefaultKeyframes[i].Copy());
                }
            }
            catch (Exception ex)
            {
                if (ProjectArrhythmia.State.InEditor)
                    EditorManager.inst.DisplayNotification($"There was an error in parsing VG Event Keyframes. Parsing got caught at {breakContext}", 4f, EditorManager.NotificationType.Error);
                if (!ProjectArrhythmia.State.InEditor)
                    Debug.LogError($"There was an error in parsing VG Event Keyframes. Parsing got caught at {breakContext}.\n {ex}");
                else
                    Debug.LogError($"{ex}");
            }

            CoreHelper.Log($"Checking keyframe counts");
            ClampEventListValues(events);

            if (jn["events"].Count > 13 && jn["events"][13] != null && events.Count > 36)
            {
                var playerForce = events[36];
                var firstKF = playerForce[0];

                firstKF.id = LSText.randomNumString(8);
                firstKF.time = 0f;
                firstKF.SetValues(jn["events"][13][0]["ev"][0].AsFloat, jn["events"][13][0]["ev"][1].AsFloat);

                for (int i = 1; i < jn["events"][13].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"][13][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.time = kfjn["t"].AsFloat;
                    eventKeyframe.SetValues(
                        kfjn["ev"][0].AsFloat,
                        kfjn["ev"][1].AsFloat);

                    events[36].Add(eventKeyframe);
                }
            }

            //ConvertedGameData = gameData;
        }

        public override void ReadJSON(JSONNode jn)
        {
            //LastParsedJSON = jn;
            var parseOptimizations = CoreConfig.Instance.ParseOptimizations.Value;

            if (jn["ordmod"] != null)
                OrderModifiers = jn["ordmod"].AsBool;

            if (jn["modifiers"] != null)
                for (int i = 0; i < jn["modifiers"].Count; i++)
                {
                    var jnModifier = jn["modifiers"][i];

                    if (jnModifier["action"] != null)
                    {
                        var triggerModifier = Modifier.Parse(jnModifier["trigger"]);
                        triggerModifier.triggerCount = jnModifier["retrigger"].AsInt;
                        var actionModifier = Modifier.Parse(jnModifier["action"]);

                        modifiers.Add(triggerModifier);
                        modifiers.Add(actionModifier);
                        continue;
                    }

                    var modifier = Modifier.Parse(jnModifier);
                    ModifiersHelper.AssignModifierFunctions(modifier, ModifierReferenceType.GameData);
                    if (ModifiersHelper.VerifyModifier(modifier, ModifiersManager.inst.defaultLevelModifiers))
                        modifiers.Add(modifier);
                }

            modifierBlocks = Parser.ParseModifierBlocks(jn["modifier_blocks"], ModifierReferenceType.ModifierBlock);

            data = BeatmapData.Parse(jn);

            prefabs.Clear();
            for (int i = 0; i < jn["prefabs"].Count; i++)
            {
                var prefab = Prefab.Parse(jn["prefabs"][i]);
                if (!prefabs.Has(x => x.id == prefab.id))
                    prefabs.Add(prefab);
            }

            prefabObjects.Clear();
            for (int i = 0; i < jn["prefab_objects"].Count; i++)
            {
                var prefabObject = PrefabObject.Parse(jn["prefab_objects"][i]);
                if (!prefabObjects.Has(x => x.id == prefabObject.id) && prefabObject.GetPrefab(prefabs))
                    prefabObjects.Add(prefabObject);
            }

            beatmapThemes.Clear();
            for (int i = 0; i < jn["themes"].Count; i++)
            {
                if (string.IsNullOrEmpty(jn["themes"][i]["id"]))
                    continue;

                beatmapThemes.Add(BeatmapTheme.Parse(jn["themes"][i]));
            }

            beatmapObjects.Clear();
            for (int i = 0; i < jn["beatmap_objects"].Count; i++)
            {
                var beatmapObject = BeatmapObject.Parse(jn["beatmap_objects"][i]);

                switch (DuplicateBehavior)
                {
                    case DuplicateBehaviorType.Remove: {
                            // remove objects with duplicate ID's due to a stupid dev branch bug
                            if (beatmapObjects.TryFindIndex(x => x.id == beatmapObject.id, out int index))
                                beatmapObjects.RemoveAt(index);

                            break;
                        }
                    case DuplicateBehaviorType.DontAdd: {
                            // don't add objects with duplicate ID's due to a stupid dev branch bug
                            if (beatmapObjects.TryFindIndex(x => x.id == beatmapObject.id, out int index))
                                continue;

                            break;
                        }
                    case DuplicateBehaviorType.Keep: {
                            break;
                        }
                }

                beatmapObjects.Add(beatmapObject);
            }

            if (parseOptimizations)
                for (int i = 0; i < beatmapObjects.Count; i++)
                    beatmapObjects[i].SetAutokillToScale(beatmapObjects);

            assets.Clear();
            if (jn["assets"] != null)
                assets.ReadJSON(jn["assets"]);

            if (jn["bg_data"] != null)
                mainBackgroundLayer = jn["bg_data"]["main_layer"].AsInt;

            backgroundLayers.Clear();
            if (jn["bg_layers"] != null)
                for (int i = 0; i < jn["bg_layers"].Count; i++)
                    backgroundLayers.Add(BackgroundLayer.Parse(jn["bg_layers"][i]));

            backgroundObjects.Clear();
            for (int i = 0; i < jn["bg_objects"].Count; i++)
                backgroundObjects.Add(BackgroundObject.Parse(jn["bg_objects"][i]));

            events = ParseEventkeyframes(jn["events"], false);

            // Fix for some levels having a Y value in shake, resulting in a shake with a 0 x direction value.
            var shakeIsBroke = false;
            if (jn["events"]["shake"] != null)
                for (int i = 0; i < jn["events"]["shake"].Count; i++)
                {
                    if (jn["events"]["shake"][i]["y"] != null && jn["events"]["shake"][i]["z"] == null)
                        shakeIsBroke = true;
                }

            if (shakeIsBroke)
                for (int i = 0; i < events[3].Count; i++)
                    events[3][i].SetValue(1, 1f);

            try
            {
                if (data && data.level && Version.TryParse(data.level.modVersion, out Version modVersion) && modVersion < new Version(1, 3, 4))
                {
                    for (int i = 0; i < events[3].Count; i++)
                        events[3][i].SetValue(3, 0f);
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            ClampEventListValues(events);

            animations.Clear();
            if (jn["anims"] != null)
                for (int i = 0; i < jn["anims"].Count; i++)
                    animations.Add(PAAnimation.Parse(jn["anims"][i]));

            variables.Clear();
            if (jn["vars"] != null)
                for (int i = 0; i < jn["vars"].Count; i++)
                    variables.Add(BeatmapVariable.Parse(jn["vars"][i]));
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

            jn["editor"]["bpm"]["snap"]["objects"] = true;
            jn["editor"]["bpm"]["bpm_value"] = 140f;
            jn["editor"]["bpm"]["bpm_offset"] = 0f;
            jn["editor"]["bpm"]["BPMValue"] = 140f;
            jn["editor"]["grid"]["scale"]["x"] = 2f;
            jn["editor"]["grid"]["scale"]["y"] = 2f;
            jn["editor"]["general"]["complexity"] = 0;
            jn["editor"]["general"]["theme"] = 0;
            jn["editor"]["general"]["test_mode"] = 0;
            jn["editor"]["preview"]["cam_zoom_offset"] = 0f;
            jn["editor"]["preview"]["cam_zoom_offset_color"] = 0;

            for (int i = 0; i < 6; i++)
                jn["editor_prefab_spawn"][i] = new JSONObject();

            if (mainBackgroundLayer != 0)
                jn["parallax_settings"]["ml"] = mainBackgroundLayer;

            int numLayer = 1;
            for (int i = 0; i < Mathf.Clamp(backgroundLayers.Count, 0, 5); i++)
            {
                var layerID = backgroundLayers[i].id;
                var jnLayer = Parser.NewJSONObject();

                int numBG = 0;
                for (int j = 0; j < backgroundObjects.Count; j++)
                {
                    var bg = backgroundObjects[j];
                    if (bg.layer != layerID)
                        continue;

                    jnLayer["o"][numBG] = bg.ToJSONVG();
                    numBG++;
                }

                jn["parallax_settings"]["l"][i] = jnLayer;
                numLayer++;
            }

            // other layers are required.
            while (numLayer < 6)
            {
                jn["parallax_settings"]["l"][numLayer - 1]["d"] = 100 * numLayer;
                jn["parallax_settings"]["l"][numLayer - 1]["c"] = 1 * numLayer;
                numLayer++;
            }

            Modifier firstTrigger = null;
            int numModifier = 0;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];

                if (modifier.type == Modifier.Type.Trigger && firstTrigger == null)
                {
                    firstTrigger = modifier;
                    continue;
                }

                if (i == modifiers.Count - 1) // no following actions were found
                    continue;

                if (firstTrigger == null) // no previous triggers to associate with the action
                    continue;

                if (modifier.type != Modifier.Type.Action) // modifier needs to be an action in order to save
                    continue;

                var jnTrigger = Parser.NewJSONObject();

                var triggerIndex = ModifiersHelper.GetLevelTriggerType(firstTrigger.Name);
                var actionIndex = ModifiersHelper.GetLevelActionType(modifier.Name);

                if (triggerIndex < 0 || actionIndex < 0)
                    continue;

                jnTrigger["event_trigger"] = triggerIndex;
                jnTrigger["event_trigger_time"]["x"] = firstTrigger.GetFloat(0, 0f);
                jnTrigger["event_trigger_time"]["y"] = firstTrigger.GetFloat(0, 0f);
                jnTrigger["event_retrigger"] = firstTrigger.triggerCount;

                jnTrigger["event_type"] = actionIndex;

                for (int j = 0; j < modifier.values.Count; i++)
                    jnTrigger["event_data"][j] = modifier.GetValue(j);

                jn["triggers"][numModifier] = jnTrigger;
                numModifier++;
            }

            for (int i = 0; i < data.checkpoints.Count; i++)
                jn["checkpoints"][i] = data.checkpoints[i].ToJSONVG();

            var beatmapObjects = this.beatmapObjects.FindAll(x => !x.FromPrefab);
            for (int i = 0; i < beatmapObjects.Count; i++)
                jn["objects"][i] = beatmapObjects[i].ToJSONVG();

            var prefabObjects = this.prefabObjects.FindAll(x => !x.fromModifier && !x.FromPrefab);
            if (!prefabObjects.IsEmpty())
                for (int i = 0; i < prefabObjects.Count; i++)
                    jn["prefab_objects"][i] = prefabObjects[i].ToJSONVG();
            else
                jn["prefab_objects"] = new JSONArray();

            var prefabs = this.prefabs.FindAll(x => !x.FromPrefab);
            if (!prefabs.IsEmpty())
                for (int i = 0; i < prefabs.Count; i++)
                    jn["prefabs"][i] = prefabs[i].ToJSONVG();
            else
                jn["prefabs"] = new JSONArray();

            var idsConverter = new Dictionary<string, string>();

            int themeIndex = 0;
            if (!beatmapThemes.IsEmpty())
                foreach (var beatmapTheme in beatmapThemes)
                {
                    beatmapTheme.VGID = LSText.randomString(16);

                    if (!idsConverter.ContainsKey(Parser.TryParse(beatmapTheme.id, 0f).ToString()))
                        idsConverter.Add(Parser.TryParse(beatmapTheme.id, 0f).ToString(), beatmapTheme.VGID);

                    jn["themes"][themeIndex] = beatmapTheme.ToJSONVG();
                    themeIndex++;
                }
            else
                jn["themes"] = new JSONArray();

            if (!data.markers.IsEmpty())
                for (int i = 0; i < data.markers.Count; i++)
                    jn["markers"][i] = data.markers[i].ToJSONVG();
            else
                jn["markers"] = new JSONArray();

            // Event Handlers
            {
                // Move
                for (int i = 0; i < events[0].Count; i++)
                {
                    var eventKeyframe = events[0][i];
                    jn["events"][0][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][0][i]["t"] = eventKeyframe.time;
                    jn["events"][0][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][0][i]["ev"][1] = eventKeyframe.values[1];
                }

                // Zoom
                for (int i = 0; i < events[1].Count; i++)
                {
                    var eventKeyframe = events[1][i];
                    jn["events"][1][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][1][i]["t"] = eventKeyframe.time;
                    jn["events"][1][i]["ev"][0] = eventKeyframe.values[0];
                }

                // Rotate
                for (int i = 0; i < events[2].Count; i++)
                {
                    var eventKeyframe = events[2][i];
                    jn["events"][2][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][2][i]["t"] = eventKeyframe.time;
                    jn["events"][2][i]["ev"][0] = eventKeyframe.values[0];
                }

                // Shake
                for (int i = 0; i < events[3].Count; i++)
                {
                    var eventKeyframe = events[3][i];
                    jn["events"][3][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][3][i]["t"] = eventKeyframe.time;
                    jn["events"][3][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][3][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][3][i]["ev"][2] = eventKeyframe.values[2];
                }

                // Themes
                for (int i = 0; i < events[4].Count; i++)
                {
                    var eventKeyframe = events[4][i];
                    jn["events"][4][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][4][i]["t"] = eventKeyframe.time;
                    jn["events"][4][i]["evs"][0] = idsConverter.TryGetValue(eventKeyframe.values[0].ToString(), out string themeID) ? themeID : eventKeyframe.values[0].ToString();
                }

                // Chroma
                for (int i = 0; i < events[5].Count; i++)
                {
                    var eventKeyframe = events[5][i];
                    jn["events"][5][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][5][i]["t"] = eventKeyframe.time;
                    jn["events"][5][i]["ev"][0] = eventKeyframe.values[0];
                }

                // Bloom
                for (int i = 0; i < events[6].Count; i++)
                {
                    var eventKeyframe = events[6][i];
                    jn["events"][6][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][6][i]["t"] = eventKeyframe.time;
                    jn["events"][6][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][6][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][6][i]["ev"][2] = Mathf.Clamp(eventKeyframe.values[4], 0f, 9f);
                }

                // Vignette
                for (int i = 0; i < events[7].Count; i++)
                {
                    var eventKeyframe = events[7][i];
                    jn["events"][7][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][7][i]["t"] = eventKeyframe.time;
                    jn["events"][7][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][7][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][7][i]["ev"][2] = eventKeyframe.values[2];
                    jn["events"][7][i]["ev"][3] = eventKeyframe.values[3];
                    jn["events"][7][i]["ev"][4] = eventKeyframe.values[4];
                    jn["events"][7][i]["ev"][5] = eventKeyframe.values[5];
                    jn["events"][7][i]["ev"][6] = Mathf.Clamp(eventKeyframe.values[6], 0f, 9f);
                }

                // Lens
                for (int i = 0; i < events[8].Count; i++)
                {
                    var eventKeyframe = events[8][i];
                    jn["events"][8][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][8][i]["t"] = eventKeyframe.time;
                    jn["events"][8][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][8][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][8][i]["ev"][2] = eventKeyframe.values[2];
                }

                // Grain
                for (int i = 0; i < events[9].Count; i++)
                {
                    var eventKeyframe = events[9][i];
                    jn["events"][9][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][9][i]["t"] = eventKeyframe.time;
                    jn["events"][9][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][9][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][9][i]["ev"][2] = eventKeyframe.values[2];
                    jn["events"][9][i]["ev"][3] = 1f;
                }

                // Gradient
                for (int i = 0; i < events[15].Count; i++)
                {
                    var eventKeyframe = events[15][i];
                    jn["events"][10][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][10][i]["t"] = eventKeyframe.time;
                    jn["events"][10][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][10][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][10][i]["ev"][2] = Mathf.Clamp(eventKeyframe.values[2], 0f, 9f);
                    jn["events"][10][i]["ev"][3] = Mathf.Clamp(eventKeyframe.values[3], 0f, 9f);
                    jn["events"][10][i]["ev"][4] = eventKeyframe.values[4];
                }

                jn["events"][11][0]["ct"] = "Linear";
                jn["events"][11][0]["t"] = 0f;
                jn["events"][11][0]["ev"][0] = 0f;
                jn["events"][11][0]["ev"][1] = 0f;
                jn["events"][11][0]["ev"][2] = 0f;

                // Hueshift
                for (int i = 0; i < events[10].Count; i++)
                {
                    var eventKeyframe = events[10][i];
                    jn["events"][12][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][12][i]["t"] = eventKeyframe.time;
                    jn["events"][12][i]["ev"][0] = eventKeyframe.values[0];
                }

                // Player
                for (int i = 0; i < events[36].Count; i++)
                {
                    var eventKeyframe = events[36][i];
                    jn["events"][13][i]["ct"] = eventKeyframe.curve.ToString();
                    jn["events"][13][i]["t"] = eventKeyframe.time;
                    jn["events"][13][i]["ev"][0] = eventKeyframe.values[0];
                    jn["events"][13][i]["ev"][1] = eventKeyframe.values[1];
                    jn["events"][13][i]["ev"][2] = 0f;
                }
            }

            return jn;
        }

        public override JSONNode ToJSON()
        {
            CoreHelper.Log("Saving Beatmap");
            var jn = Parser.NewJSONObject();

            CoreHelper.Log("Saving Editor Data");
            jn["ed"]["timeline_pos"] = "0";

            CoreHelper.Log("Saving Markers");
            for (int i = 0; i < data.markers.Count; i++)
                jn["ed"]["markers"][i] = data.markers[i].ToJSON();

            this.WriteModifiersJSON(jn);

            for (int i = 0; i < modifierBlocks.Count; i++)
            {
                var modifierBlock = modifierBlocks[i];
                var jnModifierBlock = modifierBlock.ToJSON();
                jnModifierBlock["name"] = modifierBlock.Name;
                jn["modifier_blocks"][i] = jnModifierBlock;
            }

            if (!modifierBlocks.IsEmpty())
                jn["modifier_blocks"] = Parser.ModifierBlocksToJSON(modifierBlocks);

            if (assets && !assets.IsEmpty())
                jn["assets"] = assets.ToJSON();

            CoreHelper.Log("Saving Object Prefabs");
            var prefabObjects = this.prefabObjects.FindAll(x => !x.fromModifier && !x.FromPrefab);
            for (int i = 0; i < prefabObjects.Count; i++)
                jn["prefab_objects"][i] = prefabObjects[i].ToJSON();

            CoreHelper.Log("Saving Level Data");
            jn["level_data"] = data.level.ToJSON();

            CoreHelper.Log("Saving prefabs");
            var prefabs = this.prefabs.FindAll(x => !x.FromPrefab);
            for (int i = 0; i < prefabs.Count; i++)
                jn["prefabs"][i] = prefabs[i].ToJSON();

            CoreHelper.Log($"Saving themes");
            for (int i = 0; i < beatmapThemes.Count; i++)
                jn["themes"][i] = beatmapThemes[i].ToJSON();

            CoreHelper.Log("Saving Checkpoints");
            for (int i = 0; i < data.checkpoints.Count; i++)
                jn["checkpoints"][i] = data.checkpoints[i].ToJSON();

            CoreHelper.Log("Saving Beatmap Objects");
            var beatmapObjects = this.beatmapObjects.FindAll(x => !x.FromPrefab);
            for (int i = 0; i < beatmapObjects.Count; i++)
                jn["beatmap_objects"][i] = beatmapObjects[i].ToJSON();

            if (mainBackgroundLayer != 0)
                jn["bg_data"]["main_layer"] = mainBackgroundLayer;

            CoreHelper.Log("Saving Background Layers");
            var backgroundLayers = this.backgroundLayers.FindAll(x => !x.FromPrefab);
            for (int i = 0; i < backgroundLayers.Count; i++)
                jn["bg_layers"][i] = backgroundLayers[i].ToJSON();

            CoreHelper.Log("Saving Background Objects");
            var backgroundObjects = this.backgroundObjects.FindAll(x => !x.FromPrefab);
            for (int i = 0; i < backgroundObjects.Count; i++)
                jn["bg_objects"][i] = backgroundObjects[i].ToJSON();

            CoreHelper.Log("Saving Event Objects");
            for (int i = 0; i < animations.Count; i++)
                jn["anims"][i] = animations[i].ToJSON();

            var index = 0;
            for (int i = 0; i < variables.Count; i++)
            {
                var variable = variables[i];
                if (string.IsNullOrEmpty(variable.name))
                    continue;
                jn["vars"][index] = variable.ToJSON();
                index++;
            }

            for (int i = 0; i < events.Count; i++)
                for (int j = 0; j < events[i].Count; j++)
                    if (EventLibrary.jsonNames.Length > i)
                        jn["events"][EventLibrary.jsonNames[i]][j] = events[i][j].ToJSON();

            return jn;
        }

        FileFormat fileFormat;
        public FileFormat FileFormat => fileFormat;

        public string GetFileName() => $"level{FileFormat.Dot()}";

        public void ReadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var file = RTFile.ReadFromFile(path);
            if (string.IsNullOrEmpty(file))
                return;

            switch (RTFile.GetFileFormat(path))
            {
                case FileFormat.LSB: {
                        fileFormat = FileFormat.LSB;
                        ReadJSON(JSON.Parse(file));
                        break;
                    }
                case FileFormat.VGD: {
                        fileFormat = FileFormat.VGD;
                        ReadJSONVG(JSON.Parse(file));
                        break;
                    }
            }
        }

        public void WriteToFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var jn = RTFile.GetFileFormat(path) switch
            {
                FileFormat.LSB => ToJSON(),
                FileFormat.VGD => ToJSONVG(),
                _ => null,
            };
            if (jn != null)
                RTFile.WriteToFile(path, jn.ToString());
        }

        /// <summary>
        /// Saves the <see cref="GameData"/> to a LS format file.
        /// </summary>
        /// <param name="path">The file to save to.</param>
        /// <param name="onSave">Function to run when saving is complete.</param>
        /// <param name="saveGameDataThemes">If the levels' themes should be written to the JSON.</param>
        public void SaveData(string path, Action onSave = null)
        {
            if (EditorConfig.Instance.SaveAsync.Value)
                CoroutineHelper.StartCoroutineAsync(ISaveData(path, onSave));
            else
                CoroutineHelper.StartCoroutine(ISaveData(path, onSave));
        }

        /// <summary>
        /// Saves the <see cref="GameData"/> to a VG format file.
        /// </summary>
        /// <param name="path">The file to save to.</param>
        /// <param name="onSave">Function to run when saving is complete.</param>
        public void SaveDataVG(string path, Action onSave = null)
        {
            if (EditorConfig.Instance.SaveAsync.Value)
                CoroutineHelper.StartCoroutineAsync(ISaveDataVG(path, onSave));
            else
                CoroutineHelper.StartCoroutine(ISaveDataVG(path, onSave));
        }

        /// <summary>
        /// Saves the <see cref="GameData"/> to a LS format file.
        /// </summary>
        /// <param name="path">The file to save to.</param>
        /// <param name="onSave">Function to run when saving is complete.</param>
        /// <paramref name="saveGameDataThemes">If the levels' themes should be written to the JSON or not.</paramref>
        public IEnumerator ISaveData(string path, Action onSave = null)
        {
            var jn = ToJSON();
            CoreHelper.Log($"Saving Entire Beatmap to {path}");
            RTFile.WriteToFile(path, jn.ToString());

            yield return CielaSpike.Ninja.JumpToUnity;
            onSave?.Invoke();

            yield break;
        }

        /// <summary>
        /// Saves the <see cref="GameData"/> to a VG format file.
        /// </summary>
        /// <param name="path">The file to save to.</param>
        /// <param name="onSave">Function to run when saving is complete.</param>
        public IEnumerator ISaveDataVG(string path, Action onSave = null)
        {
            var jn = ToJSONVG();
            CoreHelper.Log($"Saving Entire Beatmap to {path}");
            RTFile.WriteToFile(path, jn.ToString());

            yield return CielaSpike.Ninja.JumpToUnity;
            onSave?.Invoke();

            yield break;
        }

        /// <summary>
        /// Parses all events from a level.
        /// </summary>
        /// <param name="jn">The LS JSON to parse.</param>
        /// <param name="clamp">If event keyframes list should be verified.</param>
        /// <returns>Returns a parsed list of event keyframes.</returns>
        public static List<List<EventKeyframe>> ParseEventkeyframes(JSONNode jn, bool clamp = true)
        {
            var allEvents = new List<List<EventKeyframe>>();

            var defaultEventKeyframes = EventLibrary.GetDefaultKeyframes();

            // here we iterate through the default event types and check if the JSON exists. This is so we don't need to have a ton of repeating code.
            for (int i = 0; i < EventLibrary.EVENT_COUNT; i++)
            {
                allEvents.Add(new List<EventKeyframe>());
                if (jn[EventLibrary.jsonNames[i]] == null)
                    continue;

                for (int j = 0; j < jn[EventLibrary.jsonNames[i]].Count; j++)
                {
                    var defaultKeyframe = defaultEventKeyframes[i];
                    allEvents[i].Add(EventKeyframe.Parse(jn[EventLibrary.jsonNames[i]][j], i, defaultKeyframe.values.Length, defaultKeyframe.randomValues.Length, defaultKeyframe.values, defaultKeyframe.randomValues));
                }
            }

            if (clamp)
                ClampEventListValues(allEvents);

            for (int i = 0; i < allEvents.Count; i++)
                allEvents[i] = allEvents[i].OrderBy(x => x.time).ToList(); // ensures the event keyframes are ordered correctly.

            return allEvents;
        }

        /// <summary>
        /// Verifies that the list of event keyframes is of the correct length.
        /// </summary>
        /// <param name="eventKeyframes">List of event keyframes to check.</param>
        public static void ClampEventListValues(List<List<EventKeyframe>> eventKeyframes)
        {
            // first, check if event keyframes count is higher than normal.
            while (eventKeyframes.Count > EventLibrary.EVENT_COUNT)
                eventKeyframes.RemoveAt(eventKeyframes.Count - 1);

            for (int type = 0; type < EventLibrary.EVENT_COUNT; type++)
            {
                // add to the event types if no event exists.
                if (eventKeyframes.Count < type + 1)
                    eventKeyframes.Add(new List<EventKeyframe>());

                // add an event if the list contains none.
                if (eventKeyframes[type].Count < 1)
                    eventKeyframes[type].Add(EventLibrary.cachedDefaultKeyframes[type].Copy());

                // verify the event value lengths are correct.
                for (int index = 0; index < eventKeyframes[type].Count; index++)
                {
                    var array = eventKeyframes[type][index].values;
                    if (array.Length != EventLibrary.cachedDefaultKeyframes[type].values.Length)
                    {
                        array = new float[EventLibrary.cachedDefaultKeyframes[type].values.Length];
                        for (int i = 0; i < EventLibrary.cachedDefaultKeyframes[type].values.Length; i++)
                            array[i] = i < eventKeyframes[type][index].values.Length ? eventKeyframes[type][index].values[i] : EventLibrary.cachedDefaultKeyframes[type].values[i];
                    }
                    eventKeyframes[type][index].values = array;
                }
            }
        }

        /// <summary>
        /// Gets an event keyframe at an index coordinate.
        /// </summary>
        /// <param name="keyframeCoord">Coordinates of the keyframe to get.</param>
        /// <returns>Returns a found event keyframe.</returns>
        public EventKeyframe GetEventKeyframe(KeyframeCoord keyframeCoord) => events[keyframeCoord.type][keyframeCoord.index];

        /// <summary>
        /// Gets a variable from the level.
        /// </summary>
        /// <param name="name">Name of the variable to get.</param>
        /// <returns>Returns a found variable.</returns>
        public BeatmapVariable GetVariable(string name) => variables.Find(x => x.name == name);

        /// <summary>
        /// Gets the value of a variable or a default value.
        /// </summary>
        /// <param name="name">Name of the variable to find.</param>
        /// <param name="defaultValue">Default value to return if no variable is found.</param>
        /// <returns>Returns the variable value if a matching variable is found, otherwise returns the default value.</returns>
        public string GetVariableOrDefault(string name, string defaultValue) => variables.TryFind(x => x.name == name, out BeatmapVariable variable) ? variable.value : defaultValue;

        public Assets GetAssets() => assets;

        public IRTObject GetRuntimeObject() => null;

        public IPrefabable AsPrefabable() => null;
        public ITransformable AsTransformable() => null;

        public ModifierLoop GetModifierLoop() => RTLevel.Current?.loop;

        #region Helpers

        public void Clear()
        {
            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var beatmapObject = beatmapObjects[i];
                for (int j = 0; j < 0; j++)
                {
                    var modifier = beatmapObject.modifiers[j];
                    modifier.Action = null;
                    modifier.Trigger = null;
                    modifier.Inactive = null;
                    ModifiersHelper.OnRemoveCache(modifier);
                    modifier.Result = null;
                }
            }
        }

        public List<BeatmapTheme> GetUsedThemes() => GetUsedThemes(beatmapThemes);

        public List<BeatmapTheme> GetUsedThemes(List<BeatmapTheme> beatmapThemes) => events == null || events.Count <= 4 ? new List<BeatmapTheme>() : beatmapThemes.Where(x => ThemeIsUsed(x)).ToList();

        public bool ThemeIsUsed(BeatmapTheme beatmapTheme) => Parser.TryParse(beatmapTheme.id, 0) != 0 && events[4].Has(y => y.values[0] == Parser.TryParse(beatmapTheme.id, 0));

        /// <summary>
        /// Gets a Prefab from a specific source.
        /// </summary>
        /// <param name="findType">Type to search.
        /// <br>0 = Index</br>
        /// <br>1 = Name</br>
        /// <br>2 = ID</br></param>
        /// <param name="reference">Reference to find a Prefab by.</param>
        /// <returns>Returns a found prefab.</returns>
        public Prefab GetPrefab(int findType, string reference) => findType switch
        {
            0 => prefabs.GetAt(Parser.TryParse(reference, -1)),
            1 => prefabs.Find(x => x.name == reference),
            2 => prefabs.Find(x => x.id == reference),
            _ => null,
        };

        /// <summary>
        /// Gets closest event keyframe to current time.
        /// </summary>
        /// <param name="_type">Event Keyframe Type</param>
        /// <returns>Event Keyframe Index</returns>
        public int ClosestEventKeyframe(int type)
        {
            float time = AudioManager.inst.CurrentAudioSource.time;
            if (events[type].TryFindIndex(x => x.time > time, out int nextKF))
            {
                var prevKF = nextKF - 1;

                if (nextKF == 0)
                    prevKF = 0;
                else
                {
                    var v1 = new Vector2(events[type][prevKF].time, 0f);
                    var v2 = new Vector2(events[type][nextKF].time, 0f);

                    float dis = Vector2.Distance(v1, v2) / 2f;

                    bool prevClose = time > dis + events[type][prevKF].time;
                    bool nextClose = time < events[type][nextKF].time - dis;

                    if (!prevClose)
                        return prevKF;
                    if (!nextClose)
                        return nextKF;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the objects' associated object package.
        /// </summary>
        /// <param name="modifier">Modifier reference.</param>
        /// <param name="prefabable">Prefabable reference.</param>
        /// <returns>Returns the <see cref="IBeatmap"/> associated with the <paramref name="prefabable"/>.</returns>
        public IBeatmap GetBeatmap(Modifier modifier, IPrefabable prefabable) => modifier.subPrefab && prefabable is PrefabObject prefabObject && prefabObject.runtimeObject ? prefabObject.runtimeObject.Spawner : this;

        /// <summary>
        /// Tries to get an object with a modifier's tag group.
        /// </summary>
        /// <param name="prefabInstanceOnly">If the object should only be associated with the prefab of the object.</param>
        /// <param name="groupAlive">If the object should only be alive.</param>
        /// <param name="prefabable">Prefabable object.</param>
        /// <param name="tag">Tag group.</param>
        /// <param name="result">Object result.</param>
        /// <returns>Returns true if an object was found, otherwise returns false.</returns>
        public bool TryFindObjectWithTag(Modifier modifier, IPrefabable prefabable, string tag, out BeatmapObject result)
        {
            result = FindObjectWithTag(modifier, prefabable, tag);
            return result;
        }

        /// <summary>
        /// Gets an object with a tag group.
        /// </summary>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns the found object.</returns>
        public BeatmapObject FindObjectWithTag(string tag) => FindObjectWithTag(beatmapObjects, tag);

        /// <summary>
        /// Gets an object with a modifier's tag group.
        /// </summary>
        /// <param name="prefabInstanceOnly">If the object should only be associated with the prefab of the object.</param>
        /// <param name="groupAlive">If the object should only be alive.</param>
        /// <param name="prefabable">Prefabable object.</param>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns the found object.</returns>
        public BeatmapObject FindObjectWithTag(Modifier modifier, IPrefabable prefabable, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return prefabable as BeatmapObject;

            var beatmap = GetBeatmap(modifier, prefabable);
            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);
            return beatmap.BeatmapObjects.Find(x => (!modifier.groupAlive || x.Alive) && x.tags.Contains(tag) && (!prefabInstanceOnly || x.SamePrefabInstance(prefabable)));
        }

        /// <summary>
        /// Gets an object with a tag group.
        /// </summary>
        /// <param name="beatmapObjects">Objects list to search.</param>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns the found object.</returns>
        public BeatmapObject FindObjectWithTag(List<BeatmapObject> beatmapObjects, string tag) => beatmapObjects.Find(x => x.tags.Contains(tag));

        /// <summary>
        /// Tries to get an object with a modifier's tag group.
        /// </summary>
        /// <param name="prefabInstanceOnly">If the object should only be associated with the prefab of the object.</param>
        /// <param name="groupAlive">If the object should only be alive.</param>
        /// <param name="prefabable">Prefabable object.</param>
        /// <param name="tag">Tag group.</param>
        /// <param name="result">Object result.</param>
        /// <returns>Returns true if an object was found, otherwise returns false.</returns>
        public bool TryFindPrefabableWithTag(Modifier modifier, IPrefabable prefabable, string tag, out IPrefabable result)
        {
            result = FindPrefabableWithTag(modifier, prefabable, tag);
            return result != null;
        }

        /// <summary>
        /// Gets an object with a modifier's tag group.
        /// </summary>
        /// <param name="prefabInstanceOnly">If the object should only be associated with the prefab of the object.</param>
        /// <param name="groupAlive">If the object should only be alive.</param>
        /// <param name="prefabable">Prefabable object.</param>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns the found object.</returns>
        public IPrefabable FindPrefabableWithTag(Modifier modifier, IPrefabable prefabable, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return prefabable;

            var beatmap = GetBeatmap(modifier, prefabable);
            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);
            return beatmap.GetPrefabables().FirstOrDefault(x => (!modifier.groupAlive || x is ILifetime akt && akt.Alive) && x is IModifyable modifyable && modifyable.Tags.Contains(tag) && (!prefabInstanceOnly || x.SamePrefabInstance(prefabable)));
        }

        /// <summary>
        /// Tries to get an object with a modifier's tag group.
        /// </summary>
        /// <param name="prefabInstanceOnly">If the object should only be associated with the prefab of the object.</param>
        /// <param name="groupAlive">If the object should only be alive.</param>
        /// <param name="prefabable">Prefabable object.</param>
        /// <param name="tag">Tag group.</param>
        /// <param name="result">Object result.</param>
        /// <returns>Returns true if an object was found, otherwise returns false.</returns>
        public bool TryFindPrefabObjectWithTag(Modifier modifier, IPrefabable prefabable, string tag, out PrefabObject result)
        {
            result = FindPrefabObjectWithTag(modifier, prefabable, tag);
            return result;
        }

        /// <summary>
        /// Gets an object with a modifier's tag group.
        /// </summary>
        /// <param name="prefabInstanceOnly">If the object should only be associated with the prefab of the object.</param>
        /// <param name="groupAlive">If the object should only be alive.</param>
        /// <param name="prefabable">Prefabable object.</param>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns the found object.</returns>
        public PrefabObject FindPrefabObjectWithTag(Modifier modifier, IPrefabable prefabable, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return prefabable as PrefabObject;

            var beatmap = GetBeatmap(modifier, prefabable);
            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);
            return beatmap.PrefabObjects.Find(x => (!modifier.groupAlive || x.Alive) && x.tags.Contains(tag) && (!prefabInstanceOnly || x.SamePrefabInstance(prefabable)));
        }

        /// <summary>
        /// Gets a list of objects with a tag group.
        /// </summary>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns a list of found objects.</returns>
        public List<BeatmapObject> FindObjectsWithTag(string tag) => FindObjectsWithTag(beatmapObjects, tag);

        /// <summary>
        /// Gets a list of objects with a tag group.
        /// </summary>
        /// <param name="prefabInstanceOnly">If the objects should only be associated with the prefab of the object.</param>
        /// <param name="groupAlive">If the objects should only be alive.</param>
        /// <param name="prefabable">Prefabable object.</param>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns a list of found objects.</returns>
        public List<BeatmapObject> FindObjectsWithTag(Modifier modifier, IPrefabable prefabable, string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return new List<BeatmapObject>() { prefabable as BeatmapObject };

            var beatmapObjects = modifier.subPrefab && prefabable is PrefabObject prefabObject && prefabObject.runtimeObject ? prefabObject.runtimeObject.Spawner.BeatmapObjects : this.beatmapObjects;

            var beatmap = GetBeatmap(modifier, prefabable);
            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);
            return beatmap.BeatmapObjects.FindAll(x => (!modifier.groupAlive || x.Alive) && x.tags.Contains(tag) && (!prefabInstanceOnly || x.SamePrefabInstance(prefabable)));
        }

        /// <summary>
        /// Gets a list of objects with a tag group.
        /// </summary>
        /// <param name="beatmapObjects">Objects list to search.</param>
        /// <param name="tag">Tag group.</param>
        /// <returns>Returns a list of found objects.</returns>
        public List<BeatmapObject> FindObjectsWithTag(List<BeatmapObject> beatmapObjects, string tag) => beatmapObjects.FindAll(x => x.tags.Contains(tag));

        public List<IPrefabable> FindPrefabablesWithTag(Modifier modifier, IPrefabable prefabable, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                var list = new List<IPrefabable>(1);
                list.Add(prefabable);
                return list;
            }

            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);
            IBeatmap beatmap = GetBeatmap(modifier, prefabable);
            return beatmap.FindPrefabablesList(x => (!modifier.groupAlive || x is ILifetime akt && akt.Alive) &&
                    x is IModifyable modifyable && modifyable.Tags.Contains(tag) && (!prefabInstanceOnly || x.SamePrefabInstance(prefabable)));
        }

        public List<IModifyable> FindModifyablesWithTag(Modifier modifier, IPrefabable prefabable, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                var list = new List<IModifyable>(1);
                if (prefabable is IModifyable modifyable)
                    list.Add(modifyable);
                return list;
            }

            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);
            IBeatmap beatmap = GetBeatmap(modifier, prefabable);
            return beatmap.FindModifyablesList(x => (!modifier.groupAlive || x is ILifetime akt && akt.Alive) &&
                    x.Tags.Contains(tag) && (!prefabInstanceOnly || x is IPrefabable p && p.SamePrefabInstance(prefabable)));
        }

        public List<ITransformable> FindTransformablesWithTag(Modifier modifier, IPrefabable prefabable, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                var list = new List<ITransformable>(1);
                if (prefabable is ITransformable transformable)
                    list.Add(transformable);
                return list;
            }

            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);
            IBeatmap beatmap = GetBeatmap(modifier, prefabable);
            return beatmap.FindTransformablesList(x => (!modifier.groupAlive || x is ILifetime akt && akt.Alive) &&
                    x is IModifyable modifyable && modifyable.Tags.Contains(tag) && (!prefabInstanceOnly || x is IPrefabable p && p.SamePrefabInstance(prefabable)));
        }

        public List<IParentable> FindParentablesWithTag(Modifier modifier, IPrefabable prefabable, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                var list = new List<IParentable>(1);
                if (prefabable is IParentable parentable)
                    list.Add(parentable);
                return list;
            }

            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);
            IBeatmap beatmap = GetBeatmap(modifier, prefabable);
            return beatmap.FindParentablesList(parentable => (!modifier.groupAlive || parentable is ILifetime akt && akt.Alive || parentable is ILifetime pakt && pakt.Alive) &&
                    parentable is IModifyable modifyable && modifyable.Tags.Contains(tag) &&
                    (!prefabInstanceOnly || parentable is IPrefabable p && p.SamePrefabInstance(prefabable)));
        }

        public IEnumerable<ITransformable> FindTransformables(Modifier modifier, IPrefabable prefabable, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                if (prefabable is ITransformable transformable)
                    yield return transformable;
                yield break;
            }

            IBeatmap beatmap = GetBeatmap(modifier, prefabable);
            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);

            var transformables = beatmap.GetTransformables();
            foreach (var transformable in transformables)
            {
                if ((!modifier.groupAlive || transformable is ILifetime akt && akt.Alive) &&
                    transformable is IModifyable modifyable && modifyable.Tags.Contains(tag) &&
                    (!prefabInstanceOnly || transformable is IPrefabable p && p.SamePrefabInstance(prefabable)))
                    yield return transformable;
            }
        }

        public bool TryFindTransformableWithTag(Modifier modifier, IPrefabable prefabable, string tag, out ITransformable result)
        {
            result = FindTransformableWithTag(modifier, prefabable, tag);
            return result is not null;
        }

        public ITransformable FindTransformableWithTag(Modifier modifier, IPrefabable prefabable, string tag)
        {
            IBeatmap beatmap = GetBeatmap(modifier, prefabable);
            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);

            var transformables = beatmap.GetTransformables();
            return string.IsNullOrEmpty(tag) ? prefabable as ITransformable : transformables.FirstOrDefault(x => (!modifier.groupAlive || x is ILifetime akt && akt.Alive) &&
                    x is IModifyable modifyable && modifyable.Tags.Contains(tag) && (!prefabInstanceOnly || x is IPrefabable p && p.SamePrefabInstance(prefabable)));
        }

        public IEnumerable<IModifyable> FindModifyables(Modifier modifier, IPrefabable prefabable, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                if (prefabable is IModifyable modifyable)
                    yield return modifyable;
                yield break;
            }

            IBeatmap beatmap = GetBeatmap(modifier, prefabable);
            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);

            var modifyables = beatmap.GetModifyables();
            foreach (var modifyable in modifyables)
            {
                if ((!modifier.groupAlive || modifyable is ILifetime akt && akt.Alive) &&
                    modifyable.Tags.Contains(tag) &&
                    (!prefabInstanceOnly || modifyable is IPrefabable p && p.SamePrefabInstance(prefabable)))
                    yield return modifyable;
            }
        }

        public bool TryFindModifyableWithTag(Modifier modifier, IPrefabable prefabable, string tag, out IModifyable result)
        {
            result = FindModifyableWithTag(modifier, prefabable, tag);
            return result is not null;
        }

        public IModifyable FindModifyableWithTag(Modifier modifier, IPrefabable prefabable, string tag)
        {
            IBeatmap beatmap = GetBeatmap(modifier, prefabable);
            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);

            var modifyables = beatmap.GetModifyables();
            return string.IsNullOrEmpty(tag) ? prefabable as IModifyable : modifyables.FirstOrDefault(x => (!modifier.groupAlive || x is ILifetime akt && akt.Alive) &&
                    x.Tags.Contains(tag) && (!prefabInstanceOnly || x is IPrefabable p && p.SamePrefabInstance(prefabable)));
        }

        public IEnumerable<IParentable> FindParentables(Modifier modifier, IPrefabable prefabable, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                if (prefabable is IParentable parentable)
                    yield return parentable;
                yield break;
            }

            IBeatmap beatmap = GetBeatmap(modifier, prefabable);
            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);

            var parentables = beatmap.GetParentables();
            foreach (var parentable in parentables)
            {
                if ((!modifier.groupAlive || parentable is ILifetime akt && akt.Alive || parentable is ILifetime pakt && pakt.Alive) &&
                    parentable is IModifyable modifyable && modifyable.Tags.Contains(tag) &&
                    (!prefabInstanceOnly || parentable is IPrefabable p && p.SamePrefabInstance(prefabable)))
                    yield return parentable;
            }
        }

        public bool TryFindParentableWithTag(Modifier modifier, IPrefabable prefabable, string tag, out IParentable result)
        {
            result = FindParentableWithTag(modifier, prefabable, tag);
            return result is not null;
        }

        public IParentable FindParentableWithTag(Modifier modifier, IPrefabable prefabable, string tag)
        {
            IBeatmap beatmap = GetBeatmap(modifier, prefabable);
            var prefabInstanceOnly = modifier.prefabInstanceOnly && !string.IsNullOrEmpty(prefabable.PrefabInstanceID);

            var modifyables = beatmap.GetParentables();
            return string.IsNullOrEmpty(tag) ? prefabable as IParentable : modifyables.FirstOrDefault(x => (!modifier.groupAlive || x is ILifetime akt && akt.Alive) &&
                    x is IModifyable modifyable && modifyable.Tags.Contains(tag) && (!prefabInstanceOnly || x is IPrefabable p && p.SamePrefabInstance(prefabable)));
        }

        public static float InterpolateFloatKeyframes(List<EventKeyframe> eventKeyframes, float time, int valueIndex, bool isLerper = true)
        {
            var list = eventKeyframes.OrderBy(x => x.time).ToList();

            var nextKFIndex = list.FindIndex(x => x.time > time);

            if (nextKFIndex < 0)
                nextKFIndex = list.Count - 1;

            var prevKFIndex = nextKFIndex - 1;
            if (prevKFIndex < 0)
                prevKFIndex = 0;

            var nextKF = list[nextKFIndex];
            var prevKF = list[prevKFIndex];

            if (prevKF.values.Length <= valueIndex)
                return 0f;

            var total = 0f;
            var prevtotal = 0f;
            for (int k = 0; k < nextKFIndex; k++)
            {
                if (list[k + 1].relative)
                    total += list[k].values[valueIndex];
                else
                    total = 0f;

                if (list[k].relative)
                    prevtotal += list[k].values[valueIndex];
                else
                    prevtotal = 0f;
            }

            var next = nextKF.relative ? total + nextKF.values[valueIndex] : nextKF.values[valueIndex];
            var prev = prevKF.relative || nextKF.relative ? prevtotal : prevKF.values[valueIndex];

            if (float.IsNaN(prev) || !isLerper)
                prev = 0f;

            if (float.IsNaN(next))
                next = 0f;

            if (!isLerper)
                next = 1f;

            if (prevKFIndex == nextKFIndex)
                return next;

            var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curve.ToString())(RTMath.InverseLerp(prevKF.time, nextKF.time, Mathf.Clamp(time, 0f, nextKF.time))));

            if (prevKFIndex == nextKFIndex)
                x = next;

            if (float.IsNaN(x) || float.IsInfinity(x))
                x = next;

            return x;
        }

        public static Vector2 InterpolateVector2Keyframes(List<EventKeyframe> eventKeyframes, float time)
        {
            var list = eventKeyframes.OrderBy(x => x.time).ToList();

            var nextKFIndex = list.FindIndex(x => x.time > time);

            if (nextKFIndex < 0)
                nextKFIndex = list.Count - 1;

            var prevKFIndex = nextKFIndex - 1;
            if (prevKFIndex < 0)
                prevKFIndex = 0;

            var nextKF = list[nextKFIndex];
            var prevKF = list[prevKFIndex];

            if (prevKF.values.Length <= 0)
                return Vector2.zero;

            Vector2 total = Vector3.zero;
            Vector2 prevtotal = Vector3.zero;
            for (int k = 0; k < nextKFIndex; k++)
            {
                if (list[k + 1].relative)
                    total += new Vector2(list[k].values[0], list[k].values[1]);
                else
                    total = Vector3.zero;

                if (list[k].relative)
                    prevtotal += new Vector2(list[k].values[0], list[k].values[1]);
                else
                    prevtotal = Vector2.zero;
            }

            var next = nextKF.relative ? total + new Vector2(nextKF.values[0], nextKF.values[1]) : new Vector2(nextKF.values[0], nextKF.values[1]);
            var prev = prevKF.relative || nextKF.relative ? prevtotal : new Vector2(prevKF.values[0], prevKF.values[1]);

            if (float.IsNaN(prev.x) || float.IsNaN(prev.y))
                prev = Vector2.zero;

            if (float.IsNaN(prev.x) || float.IsNaN(prev.y))
                next = Vector2.zero;

            if (prevKFIndex == nextKFIndex)
                return next;

            var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curve.ToString())(RTMath.InverseLerp(prevKF.time, nextKF.time, Mathf.Clamp(time, 0f, nextKF.time))));

            if (prevKFIndex == nextKFIndex)
                x = next;

            if (float.IsNaN(x.x) || float.IsNaN(x.y) || float.IsInfinity(x.x) || float.IsInfinity(x.y))
                x = next;

            return x;
        }

        public static Vector3 InterpolateVector3Keyframes(List<EventKeyframe> eventKeyframes, float time)
        {
            var list = eventKeyframes.OrderBy(x => x.time).ToList();

            var nextKFIndex = list.FindIndex(x => x.time > time);

            if (nextKFIndex < 0)
                nextKFIndex = list.Count - 1;

            var prevKFIndex = nextKFIndex - 1;
            if (prevKFIndex < 0)
                prevKFIndex = 0;

            var nextKF = list[nextKFIndex];
            var prevKF = list[prevKFIndex];

            if (prevKF.values.Length <= 0)
                return Vector3.zero;

            Vector3 total = Vector3.zero;
            Vector3 prevtotal = Vector3.zero;
            for (int k = 0; k < nextKFIndex; k++)
            {
                if (list[k + 1].relative)
                    total += new Vector3(list[k].values[0], list[k].values[1], list[k].values[2]);
                else
                    total = Vector3.zero;

                if (list[k].relative)
                    prevtotal += new Vector3(list[k].values[0], list[k].values[1], list[k].values[2]);
                else
                    prevtotal = Vector3.zero;
            }

            var next = nextKF.relative ? total + new Vector3(nextKF.values[0], nextKF.values[1], nextKF.values[2]) : new Vector3(nextKF.values[0], nextKF.values[1], nextKF.values[2]);
            var prev = prevKF.relative || nextKF.relative ? prevtotal : new Vector3(prevKF.values[0], prevKF.values[1], prevKF.values[2]);

            if (float.IsNaN(prev.x) || float.IsNaN(prev.y) || float.IsNaN(prev.z))
                prev = Vector3.zero;

            if (float.IsNaN(prev.x) || float.IsNaN(prev.y) || float.IsNaN(prev.z))
                next = Vector3.zero;

            if (prevKFIndex == nextKFIndex)
                return next;

            var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curve.ToString())(RTMath.InverseLerp(prevKF.time, nextKF.time, Mathf.Clamp(time, 0f, nextKF.time))));

            if (prevKFIndex == nextKFIndex)
                x = next;

            if (float.IsNaN(x.x) || float.IsNaN(x.y) || float.IsNaN(x.z) || float.IsInfinity(x.x) || float.IsInfinity(x.y) || float.IsInfinity(x.z))
                x = next;

            return x;
        }

        #endregion

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameData operator +(GameData a, GameData b) => Combiner.Combine(a, b);

        public static class Combiner
        {
            #region Settings

            public static bool prioritizeFirstEvents = true;
            public static bool prioritizeFirstThemes = true;

            public static bool addFirstMarkers = true;
            public static bool addSecondMarkers = false;

            public static bool addFirstCheckpoints = true;
            public static bool addSecondCheckpoints = false;

            public static bool objectsWithMatchingIDAddKeyframes = false;

            #endregion

            /// <summary>
            /// Combines multiple GameDatas together.
            /// </summary>
            /// <param name="gameDatas">Array of GameData to combine together.</param>
            /// <returns>Combined GameData.</returns>
            public static GameData Combine(params GameData[] gameDatas)
            {
                var baseData = new GameData
                {
                    data = new BeatmapData
                    {
                        level = new LevelData(),
                    },
                };

                if (gameDatas != null && gameDatas.Length > 0)
                    for (int i = 0; i < gameDatas.Length; i++)
                    {
                        if (gameDatas[i].data != null && baseData.data != null)
                        {
                            if (baseData.data.checkpoints == null)
                                baseData.data.checkpoints = new List<Checkpoint>();
                            if (baseData.data.markers == null)
                                baseData.data.markers = new List<Marker>();

                            baseData.data.checkpoints.AddRange(gameDatas[i].data.checkpoints.FindAll(x => !baseData.data.checkpoints.Has(y => y.time == x.time)));
                            baseData.data.markers.AddRange(gameDatas[i].data.markers.FindAll(x => !baseData.data.markers.Has(y => y.time == x.time)));
                        }

                        if (baseData.beatmapObjects == null)
                            baseData.beatmapObjects = new List<BeatmapObject>();

                        baseData.beatmapObjects.AddRange(gameDatas[i].beatmapObjects.FindAll(x => !baseData.beatmapObjects.Has(y => y.id == x.id)));

                        if (baseData.prefabObjects == null)
                            baseData.prefabObjects = new List<PrefabObject>();

                        baseData.prefabObjects.AddRange(gameDatas[i].prefabObjects.Where(x => !baseData.prefabObjects.Has(y => y.id == x.id)));

                        if (baseData.prefabs == null)
                            baseData.prefabs = new List<Prefab>();

                        baseData.prefabs.AddRange(gameDatas[i].prefabs.FindAll(x => !baseData.prefabs.Has(y => y.id == x.id)));

                        if (baseData.backgroundObjects == null)
                            baseData.backgroundObjects = new List<BackgroundObject>();

                        baseData.backgroundObjects.AddRange(gameDatas[i].backgroundObjects.Where(x => !baseData.backgroundObjects.Has(y =>
                        {
                            return y.active == x.active &&
                                    y.color == x.color &&
                                    y.iterations == x.iterations &&
                                    y.drawFade == x.drawFade &&
                                    y.fadeColor == x.fadeColor &&
                                    y.depth == x.depth &&
                                    y.name == x.name &&
                                    y.pos == x.pos &&
                                    y.reactiveType == x.reactiveType &&
                                    y.reactiveCol == x.reactiveCol &&
                                    y.reactiveColIntensity == x.reactiveColIntensity &&
                                    y.reactiveColSample == x.reactiveColSample &&
                                    y.reactivePosIntensity == x.reactivePosIntensity &&
                                    y.reactivePosSamples == x.reactivePosSamples &&
                                    y.reactiveRotIntensity == x.reactiveRotIntensity &&
                                    y.reactiveRotSample == x.reactiveRotSample &&
                                    y.reactiveScaIntensity == x.reactiveScaIntensity &&
                                    y.reactiveScale == x.reactiveScale &&
                                    y.reactiveScaSamples == x.reactiveScaSamples &&
                                    y.reactiveSize == x.reactiveSize &&
                                    y.reactiveZIntensity == x.reactiveZIntensity &&
                                    y.reactiveZSample == x.reactiveZSample &&
                                    y.rot == x.rot &&
                                    y.rotation == x.rotation &&
                                    y.scale == x.scale &&
                                    y.text == x.text &&
                                    y.zscale == x.zscale;
                        })));

                        if (baseData.modifierBlocks == null)
                            baseData.modifierBlocks = new List<ModifierBlock>();

                        baseData.modifierBlocks.AddRange(gameDatas[i].modifierBlocks.FindAll(x => !baseData.modifierBlocks.Has(y => y.Name == x.Name)));
                        
                        if (baseData.animations == null)
                            baseData.animations = new List<PAAnimation>();

                        baseData.animations.AddRange(gameDatas[i].animations.FindAll(x => !baseData.animations.Has(y => y.id == x.id)));

                        if (baseData.events == null)
                            baseData.events = new List<List<EventKeyframe>>();

                        for (int j = 0; j < gameDatas[i].events.Count; j++)
                        {
                            if (baseData.events.Count <= j)
                                baseData.events.Add(new List<EventKeyframe>());

                            baseData.events[j].AddRange(gameDatas[i].events[j].Where(x => !baseData.events[j].Has(y => y.time == x.time)));
                        }

                        foreach (var beatmapTheme in gameDatas[i].beatmapThemes)
                            baseData.AddTheme(beatmapTheme);

                        // Clearing
                        {
                            for (int j = 0; j < gameDatas[i].data.checkpoints.Count; j++)
                                gameDatas[i].data.checkpoints[j] = null;
                            gameDatas[i].data.checkpoints.Clear();

                            for (int j = 0; j < gameDatas[i].data.markers.Count; j++)
                                gameDatas[i].data.markers[j] = null;
                            gameDatas[i].data.markers.Clear();

                            for (int j = 0; j < gameDatas[i].beatmapObjects.Count; j++)
                                gameDatas[i].beatmapObjects[j] = null;
                            gameDatas[i].beatmapObjects.Clear();

                            for (int j = 0; j < gameDatas[i].backgroundObjects.Count; j++)
                                gameDatas[i].backgroundObjects[j] = null;
                            gameDatas[i].backgroundObjects.Clear();

                            for (int j = 0; j < gameDatas[i].prefabObjects.Count; j++)
                                gameDatas[i].prefabObjects[j] = null;
                            gameDatas[i].prefabObjects.Clear();

                            for (int j = 0; j < gameDatas[i].prefabs.Count; j++)
                                gameDatas[i].prefabs[j] = null;
                            gameDatas[i].prefabs.Clear();

                            gameDatas[i].beatmapThemes.Clear();

                            for (int j = 0; j < gameDatas[i].events.Count; j++)
                                gameDatas[i].events[j] = null;
                            gameDatas[i].events.Clear();

                            gameDatas[i] = null;
                        }
                    }

                gameDatas = null;

                return baseData;
            }
        }
    }
}