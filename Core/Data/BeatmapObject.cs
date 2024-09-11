using BetterLegacy.Components;
using BetterLegacy.Components.Editor;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor;
using LSFunctions;
using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;

namespace BetterLegacy.Core.Data
{
    public class BeatmapObject : BaseBeatmapObject
    {
        public BeatmapObject() : base()
        {
            editorData = new ObjectEditorData();
        }

        public BeatmapObject(bool active, float startTime, string name, int shape, string text, List<List<BaseEventKeyframe>> eventKeyframes) : base(active, startTime, name, shape, text, eventKeyframes)
        {
            editorData = new ObjectEditorData();
        }

        public BeatmapObject(float startTime) : base(startTime)
        {
            editorData = new ObjectEditorData();
        }

        /// <summary>
        /// If true, object does not render when the user has the <see cref="Configs.CoreConfig.LDM"/> setting on.
        /// </summary>
        public bool LDM { get; set; }

        /// <summary>
        /// Multiplies from the parents' position, allowing for parallaxing.
        /// </summary>
        public float[] parallaxSettings = new float[3]
        {
            1f, // Pos
            1f, // Sca
            1f, // Rot
        };

        /// <summary>
        /// If parent chains should be accounted for when parent offset / delay is used.
        /// </summary>
        public string parentAdditive = "000";

        /// <summary>
        /// The tags used to identify a group of objects or object properties.
        /// </summary>
        public List<string> tags = new List<string>();

        /// <summary>
        /// The depth the visual object is rendered on.
        /// </summary>
        public new int Depth
        {
            get => depth;
            set => depth = value;
        }

        /// <summary>
        /// If true and objects' opacity is less than 100%, disables collision. Acts the same as modern Project Arrhythmia.
        /// </summary>
        public bool opacityCollision = false;

        /// <summary>
        /// If true the object should render on the background layer (perspective), otherwise render on the foreground layer (orthographic)
        /// </summary>
        public bool background;

        /// <summary>
        /// If modifiers ignore the lifespan restriction.
        /// </summary>
        public bool ignoreLifespan = false;

        /// <summary>
        /// If the object should stop following the parent chain after spawn.
        /// </summary>
        public bool desync = false;

        /// <summary>
        /// Modifiers the object contains.
        /// </summary>
        public List<Modifier<BeatmapObject>> modifiers = new List<Modifier<BeatmapObject>>();

        /// <summary>
        /// For object modifiers.
        /// </summary>
        public List<Component> components = new List<Component>();

        /// <summary>
        /// ParticleSystem for modifiers.
        /// </summary>
        public ParticleSystem particleSystem;

        /// <summary>
        /// TrailRender for modifiers.
        /// </summary>
        public TrailRenderer trailRenderer;

        /// <summary>
        /// Used for editor optimization.
        /// </summary>
        public RTObject RTObject { get; set; }

        /// <summary>
        /// Used for optimization.
        /// </summary>
        public Optimization.Objects.LevelObject levelObject;

        /// <summary>
        /// Used for editor.
        /// </summary>
        public TimelineObject timelineObject;

        /// <summary>
        /// Use for object modifiers.
        /// </summary>
        public Detector detector;

        public int integerVariable;
        public float floatVariable;
        public string stringVariable = "";

        public Vector3 reactivePositionOffset = Vector3.zero;
        public Vector3 reactiveScaleOffset = Vector3.zero;
        public float reactiveRotationOffset = 0f;

        /// <summary>
        /// Moves the objects' associated parent objects at this offset.
        /// </summary>
        public Vector3 positionOffset = Vector3.zero;

        /// <summary>
        /// Scales the objects' associated parent objects at this offset.
        /// </summary>
        public Vector3 scaleOffset = Vector3.zero;

        /// <summary>
        /// Rotates the objects' associated parent objects at this offset.
        /// </summary>
        public Vector3 rotationOffset = Vector3.zero;

        /// <summary>
        /// Used for objects spawned from a Prefab Object.
        /// </summary>
        public string originalID;

        /// <summary>
        /// Gets if the current audio time is within the lifespan of the object.
        /// </summary>
        public bool Alive
        {
            get
            {
                var time = AudioManager.inst.CurrentAudioSource.time;
                var st = StartTime;
                var akt = autoKillType;
                var ako = autoKillOffset;
                var l = GetObjectLifeLength(_oldStyle: true);
                return time >= st && (time <= l + st && akt != AutoKillType.OldStyleNoAutokill && akt != AutoKillType.SongTime || akt == AutoKillType.OldStyleNoAutokill || time < ako && akt == AutoKillType.SongTime);
            }
        }

        /// <summary>
        /// Gets the total amount of keyframes the object has.
        /// </summary>
        public int KeyframeCount
        {
            get
            {
                int result = -1;
                if (events != null && events.Count > 0)
                {
                    for (int i = 0; i < events.Count; i++)
                    {
                        if (events[i] != null && events[i].Count > 0)
                            result += events[i].Count;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Object spawn conditions.
        /// </summary>
        public new ObjectType objectType;

        public new enum ObjectType
        {
            /// <summary>
            /// Is opaque and can hit the player.
            /// </summary>
            Normal,
            /// <summary>
            /// Is forced to be transparent with no collision.
            /// </summary>
            Helper,
            /// <summary>
            /// Has no collision but is opaque like <see cref="ObjectType.Normal"/>.
            /// </summary>
            Decoration,
            /// <summary>
            /// Doesn't spawn a physical level object to be seen in-game.
            /// </summary>
            Empty,
            /// <summary>
            /// Like <see cref="ObjectType.Normal"/> except instead of getting hit the player cannot pass through the object.
            /// </summary>
            Solid
        }

        /// <summary>
        /// Gets the prefab reference.
        /// </summary>
        public Prefab Prefab => (Prefab)DataManager.inst.gameData.prefabs.Find(x => x.ID == prefabID);

        /// <summary>
        /// Gets the prefab object reference.
        /// </summary>
        public PrefabObject PrefabObject => (PrefabObject)DataManager.inst.gameData.prefabObjects.Find(x => x.ID == prefabInstanceID);

        #region Methods

        /// <summary>
        /// Creates a new Beatmap Object with all the same values as the original provided.
        /// </summary>
        /// <param name="orig">The original to copy.</param>
        /// <param name="newID">If a new ID should be generated.</param>
        /// <param name="copyVariables">If variables should be copied.</param>
        /// <returns>Returns a copied Beatmap Object.</returns>
        public static BeatmapObject DeepCopy(BeatmapObject orig, bool newID = true, bool copyVariables = true)
        {
            var beatmapObject = new BeatmapObject
            {
                id = newID ? LSText.randomString(16) : orig.id,
                parent = orig.parent,
                name = orig.name,
                active = orig.active,
                autoKillOffset = orig.autoKillOffset,
                autoKillType = orig.autoKillType,
                Depth = orig.Depth,
                editorData = new ObjectEditorData()
                {
                    Bin = orig.editorData.Bin,
                    layer = orig.editorData.layer,
                    collapse = orig.editorData.collapse,
                    locked = orig.editorData.locked
                },
                fromPrefab = orig.fromPrefab,
                objectType = orig.objectType,
                origin = orig.origin,
                prefabID = orig.prefabID,
                prefabInstanceID = orig.prefabInstanceID,
                shape = orig.shape,
                shapeOption = orig.shapeOption,
                StartTime = orig.StartTime,
                text = orig.text,
                LDM = orig.LDM,
                parentType = orig.parentType,
                parentOffsets = orig.parentOffsets.Clone(),
                parentAdditive = orig.parentAdditive,
                parallaxSettings = orig.parallaxSettings.Copy(),
                integerVariable = copyVariables ? orig.integerVariable : 0,
                floatVariable = copyVariables ? orig.floatVariable : 0f,
                stringVariable = copyVariables ? orig.stringVariable : "",
                tags = orig.tags.Count > 0 ? orig.tags.Clone() : new List<string>(),
                background = orig.background,
                ignoreLifespan = orig.ignoreLifespan,
                opacityCollision = orig.opacityCollision,
                desync = orig.desync
            };

            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                beatmapObject.events[i].AddRange(orig.events[i].Select(x => EventKeyframe.DeepCopy((EventKeyframe)x)));
            }

            beatmapObject.modifiers = new List<Modifier<BeatmapObject>>();
            beatmapObject.modifiers = orig.modifiers.Count > 0 ? orig.modifiers.Select(x => Modifier<BeatmapObject>.DeepCopy(x, beatmapObject)).ToList() : new List<Modifier<BeatmapObject>>();
            return beatmapObject;
        }

        /// <summary>
        /// Parses a Beatmap Object from VG to formatted JSON.
        /// </summary>
        /// <param name="jn">VG JSON.</param>
        /// <returns>Returns a parsed Beatmap Object.</returns>
        public static BeatmapObject ParseVG(JSONNode jn)
        {
            var beatmapObject = new BeatmapObject();

            var events = new List<List<BaseEventKeyframe>>();
            events.Add(new List<BaseEventKeyframe>());
            events.Add(new List<BaseEventKeyframe>());
            events.Add(new List<BaseEventKeyframe>());
            events.Add(new List<BaseEventKeyframe>());

            if (jn["e"] != null)
            {
                // Position
                {
                    for (int j = 0; j < jn["e"][0]["k"].Count; j++)
                    {
                        var eventKeyframe = new EventKeyframe();
                        var kfjn = jn["e"][0]["k"][j];

                        eventKeyframe.id = LSText.randomNumString(8);

                        eventKeyframe.eventTime = kfjn["t"].AsFloat;

                        if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.ContainsKey(kfjn["ct"]))
                            eventKeyframe.curveType = DataManager.inst.AnimationListDictionaryStr[kfjn["ct"]];

                        eventKeyframe.SetEventValues(
                            kfjn["ev"][0].AsFloat,
                            kfjn["ev"][1].AsFloat,
                            0f);

                        eventKeyframe.random = kfjn["r"].AsInt;

                        eventKeyframe.SetEventRandomValues(
                            kfjn["er"][0].AsFloat,
                            kfjn["er"][1].AsFloat,
                            kfjn["er"][2].AsFloat);

                        eventKeyframe.relative = false;
                        eventKeyframe.active = false;
                        events[0].Add(eventKeyframe);
                    }
                }

                // Scale
                {
                    for (int j = 0; j < jn["e"][1]["k"].Count; j++)
                    {
                        var eventKeyframe = new EventKeyframe();
                        var kfjn = jn["e"][1]["k"][j];

                        eventKeyframe.id = LSText.randomNumString(8);

                        eventKeyframe.eventTime = kfjn["t"].AsFloat;

                        if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.ContainsKey(kfjn["ct"]))
                            eventKeyframe.curveType = DataManager.inst.AnimationListDictionaryStr[kfjn["ct"]];

                        eventKeyframe.SetEventValues(
                            kfjn["ev"][0].AsFloat,
                            kfjn["ev"][1].AsFloat);

                        eventKeyframe.random = kfjn["r"].AsInt;

                        eventKeyframe.SetEventRandomValues(
                            kfjn["er"][0].AsFloat,
                            kfjn["er"][1].AsFloat,
                            kfjn["er"][2].AsFloat);

                        eventKeyframe.relative = false;
                        eventKeyframe.active = false;
                        events[1].Add(eventKeyframe);
                    }
                }

                // Rotation
                {
                    for (int j = 0; j < jn["e"][2]["k"].Count; j++)
                    {
                        var eventKeyframe = new EventKeyframe();
                        var kfjn = jn["e"][2]["k"][j];

                        eventKeyframe.id = LSText.randomNumString(8);

                        eventKeyframe.eventTime = kfjn["t"].AsFloat;

                        if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.ContainsKey(kfjn["ct"]))
                            eventKeyframe.curveType = DataManager.inst.AnimationListDictionaryStr[kfjn["ct"]];

                        eventKeyframe.SetEventValues(
                            kfjn["ev"][0].AsFloat);

                        eventKeyframe.random = kfjn["r"].AsInt;

                        eventKeyframe.SetEventRandomValues(
                            kfjn["er"][0].AsFloat,
                            kfjn["er"][1].AsFloat,
                            kfjn["er"][2].AsFloat);

                        eventKeyframe.relative = true;
                        eventKeyframe.active = false;
                        events[2].Add(eventKeyframe);
                    }
                }

                // Color
                {
                    for (int j = 0; j < jn["e"][3]["k"].Count; j++)
                    {
                        var eventKeyframe = new EventKeyframe();
                        var kfjn = jn["e"][3]["k"][j];

                        eventKeyframe.id = LSText.randomNumString(8);

                        eventKeyframe.eventTime = kfjn["t"].AsFloat;

                        if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.ContainsKey(kfjn["ct"]))
                            eventKeyframe.curveType = DataManager.inst.AnimationListDictionaryStr[kfjn["ct"]];

                        eventKeyframe.SetEventValues(
                            kfjn["ev"][0].AsFloat,
                            kfjn["ev"].Count == 1 ? 0f : (-(kfjn["ev"][1].AsFloat / 100f) + 1f),
                            0f,
                            0f,
                            0f);

                        eventKeyframe.random = kfjn["r"].AsInt;

                        eventKeyframe.SetEventRandomValues(
                            kfjn["er"][0].AsFloat,
                            kfjn["er"][1].AsFloat,
                            kfjn["er"][2].AsFloat,
                            0f);

                        eventKeyframe.relative = false;
                        eventKeyframe.active = false;
                        events[3].Add(eventKeyframe);
                    }
                }
            }

            beatmapObject.events = events;

            beatmapObject.id = jn["id"] != null ? jn["id"] : LSText.randomString(16);

            beatmapObject.opacityCollision = true;

            if (jn["pre_iid"] != null)
                beatmapObject.prefabInstanceID = jn["pre_iid"];

            if (jn["pre_id"] != null)
                beatmapObject.prefabID = jn["pre_id"];

            if (jn["p_id"] != null)
                beatmapObject.parent = jn["p_id"] == "camera" ? "CAMERA_PARENT" : jn["p_id"];

            if (jn["p_t"] != null)
                beatmapObject.parentType = jn["p_t"];
            if (jn["p_id"] == "camera")
                beatmapObject.parentType = "111";

            if (jn["p_o"] != null && jn["p_id"] != "camera")
            {
                beatmapObject.parentOffsets = new List<float>(from n in jn["p_o"].AsArray.Children
                                                              select n.AsFloat).ToList();
            }
            else if (jn["p_id"] == "camera")
            {
                beatmapObject.parentOffsets = new List<float> { 0f, 0f, 0f };
            }

            if (jn["ot"] != null)
            {
                var ot = jn["ot"].AsInt;

                beatmapObject.objectType = ot == 4 ? ObjectType.Normal : ot == 5 ? ObjectType.Decoration : ot == 6 ? ObjectType.Empty : (ObjectType)ot;
            }

            if (jn["st"] != null)
                beatmapObject.startTime = jn["st"].AsFloat;

            if (jn["n"] != null)
                beatmapObject.name = jn["n"];

            if (jn["d"] != null)
                beatmapObject.depth = jn["d"].AsInt;

            if (jn["s"] != null)
                beatmapObject.shape = jn["s"].AsInt;

            if (jn["so"] != null)
                beatmapObject.shapeOption = jn["so"].AsInt;

            if (jn["text"] != null)
                beatmapObject.text = jn["text"];

            if (jn["ak_t"] != null)
                beatmapObject.autoKillType = (AutoKillType)jn["ak_t"].AsInt;

            if (jn["ak_o"] != null)
                beatmapObject.autoKillOffset = jn["ak_o"].AsFloat;

            if (jn["o"] != null)
                beatmapObject.origin = new Vector2(jn["o"]["x"].AsFloat, jn["o"]["y"].AsFloat);

            if (jn["ed"] != null)
            {
                beatmapObject.editorData = ObjectEditorData.ParseVG(jn["ed"]);
            }

            return beatmapObject;
        }

        /// <summary>
        /// Parses a Beatmap Object from LS formatted JSON.
        /// </summary>
        /// <param name="jn">LS JSON.</param>
        /// <returns>Returns a parsed Beatmap Object.</returns>
        public static BeatmapObject Parse(JSONNode jn)
        {
            var beatmapObject = new BeatmapObject();

            var events = new List<List<BaseEventKeyframe>>();
            events.Add(new List<BaseEventKeyframe>());
            events.Add(new List<BaseEventKeyframe>());
            events.Add(new List<BaseEventKeyframe>());
            events.Add(new List<BaseEventKeyframe>());
            if (jn["events"] != null)
            {
                // Position
                for (int i = 0; i < jn["events"]["pos"].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"]["pos"][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;

                    if (kfjn["ct"] != null)
                        eventKeyframe.curveType = DataManager.inst.AnimationListDictionaryStr[kfjn["ct"]];

                    try
                    {
                        eventKeyframe.SetEventValues(kfjn["x"].AsFloat, kfjn["y"].AsFloat, kfjn["z"].AsFloat);
                    }
                    catch
                    {
                        // If all values end up as zero, then we definitely know Z axis didn't load for whatever reason.
                        eventKeyframe.SetEventValues(0f, 0f, 0f);
                    }

                    eventKeyframe.random = kfjn["r"].AsInt;
                    // rx = random x
                    // ry = random y
                    // rz = random interval
                    // rx2 = random axis
                    eventKeyframe.SetEventRandomValues(kfjn["rx"].AsFloat, kfjn["ry"].AsFloat, kfjn["rz"].AsFloat, kfjn["rx2"].AsFloat);

                    eventKeyframe.relative = !string.IsNullOrEmpty(kfjn["rel"]) && kfjn["rel"].AsBool;
                    eventKeyframe.locked = !string.IsNullOrEmpty(kfjn["l"]) && kfjn["l"].AsBool;

                    eventKeyframe.active = false;
                    events[0].Add(eventKeyframe);
                }

                // Scale
                for (int j = 0; j < jn["events"]["sca"].Count; j++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"]["sca"][j];

                    eventKeyframe.id = LSText.randomNumString(8);

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;

                    if (kfjn["ct"] != null)
                        eventKeyframe.curveType = DataManager.inst.AnimationListDictionaryStr[kfjn["ct"]];

                    eventKeyframe.SetEventValues(kfjn["x"].AsFloat, kfjn["y"].AsFloat);

                    eventKeyframe.random = kfjn["r"].AsInt;
                    eventKeyframe.SetEventRandomValues(kfjn["rx"].AsFloat, kfjn["ry"].AsFloat, kfjn["rz"].AsFloat);

                    eventKeyframe.relative = !string.IsNullOrEmpty(kfjn["rel"]) && kfjn["rel"].AsBool;
                    eventKeyframe.locked = !string.IsNullOrEmpty(kfjn["l"]) && kfjn["l"].AsBool;

                    eventKeyframe.active = false;
                    events[1].Add(eventKeyframe);
                }

                // Rotation
                for (int k = 0; k < jn["events"]["rot"].Count; k++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"]["rot"][k];

                    eventKeyframe.id = LSText.randomNumString(8);

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;

                    if (kfjn["ct"] != null)
                        eventKeyframe.curveType = DataManager.inst.AnimationListDictionaryStr[kfjn["ct"]];

                    eventKeyframe.SetEventValues(kfjn["x"].AsFloat);

                    eventKeyframe.random = kfjn["r"].AsInt;
                    eventKeyframe.SetEventRandomValues(kfjn["rx"].AsFloat, kfjn["ry"].AsFloat, kfjn["rz"].AsFloat);

                    eventKeyframe.relative = string.IsNullOrEmpty(kfjn["rel"]) || kfjn["rel"].AsBool;
                    eventKeyframe.locked = !string.IsNullOrEmpty(kfjn["l"]) && kfjn["l"].AsBool;

                    eventKeyframe.active = false;
                    events[2].Add(eventKeyframe);
                }

                // Color
                for (int l = 0; l < jn["events"]["col"].Count; l++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"]["col"][l];

                    eventKeyframe.id = LSText.randomNumString(8);

                    eventKeyframe.eventTime = kfjn["t"].AsFloat;

                    if (kfjn["ct"] != null)
                        eventKeyframe.curveType = DataManager.inst.AnimationListDictionaryStr[kfjn["ct"]];

                    // x = color slot
                    // y = opacity
                    // z = hue
                    // x2 = saturation
                    // y2 = value
                    eventKeyframe.SetEventValues(kfjn["x"].AsFloat, kfjn["y"].AsFloat, kfjn["z"].AsFloat, kfjn["x2"].AsFloat, kfjn["y2"].AsFloat);

                    // if gradient objects are implemented
                    // x = start color slot
                    // y = start opacity
                    // z = start hue
                    // x2 = start saturation
                    // y2 = start value
                    // z2 = end color slot
                    // x3 = end opacity
                    // y3 = end hue
                    // z3 = end saturation
                    // x4 = end value
                    //eventKeyframe.SetEventValues(kfjn["x"].AsFloat, kfjn["y"].AsFloat, kfjn["z"].AsFloat, kfjn["x2"].AsFloat, kfjn["y2"].AsFloat, kfjn["z2"].AsFloat, kfjn["x3"].AsFloat, kfjn["y3"].AsFloat, kfjn["z3"].AsFloat, kfjn["x4"].AsFloat);

                    eventKeyframe.random = kfjn["r"].AsInt;
                    eventKeyframe.SetEventRandomValues(kfjn["rx"].AsFloat);

                    eventKeyframe.locked = !string.IsNullOrEmpty(kfjn["l"]) && kfjn["l"].AsBool;

                    eventKeyframe.active = false;
                    events[3].Add(eventKeyframe);
                }
            }

            beatmapObject.events = events;

            beatmapObject.id = jn["id"] ?? LSText.randomString(16);

            if (jn["piid"] != null)
                beatmapObject.prefabInstanceID = jn["piid"];

            if (jn["pid"] != null)
                beatmapObject.prefabID = jn["pid"];

            if (jn["p"] != null)
                beatmapObject.parent = jn["p"];

            if (jn["pt"] != null)
                beatmapObject.parentType = jn["pt"];

            if (jn["po"] != null)
            {
                beatmapObject.parentOffsets = new List<float>(from n in jn["po"].AsArray.Children
                                                              select n.AsFloat).ToList();
            }

            if (jn["ps"] != null)
            {
                for (int i = 0; i < beatmapObject.parallaxSettings.Length; i++)
                {
                    if (jn["ps"].Count > i && jn["ps"][i] != null)
                        beatmapObject.parallaxSettings[i] = jn["ps"][i].AsFloat;
                }
            }

            if (jn["pa"] != null)
                beatmapObject.parentAdditive = jn["pa"];

            if (jn["d"] != null)
                beatmapObject.depth = jn["d"].AsInt;

            if (jn["rdt"] != null)
                beatmapObject.background = jn["rdt"].AsInt == 1;

            if (jn["opcol"] != null)
                beatmapObject.opacityCollision = jn["opcol"].AsBool;

            if (jn["iglif"] != null)
                beatmapObject.ignoreLifespan = jn["iglif"].AsBool;

            if (jn["desync"] != null && !string.IsNullOrEmpty(beatmapObject.parent))
                beatmapObject.desync = jn["desync"].AsBool;

            if (jn["empty"] != null)
                beatmapObject.objectType = jn["empty"].AsBool ? ObjectType.Empty : ObjectType.Normal;
            else if (jn["h"] != null)
                beatmapObject.objectType = jn["h"].AsBool ? ObjectType.Helper : ObjectType.Normal;
            else if (jn["ot"] != null)
                beatmapObject.objectType = (ObjectType)jn["ot"].AsInt;

            if (jn["ldm"] != null)
                beatmapObject.LDM = jn["ldm"].AsBool;

            if (jn["st"] != null)
                beatmapObject.startTime = jn["st"].AsFloat;

            if (jn["name"] != null)
                beatmapObject.name = ((string)jn["name"]).Replace("{{colon}}", ":");

            if (jn["tags"] != null)
                for (int i = 0; i < jn["tags"].Count; i++)
                    beatmapObject.tags.Add(((string)jn["tags"][i]).Replace("{{colon}}", ":"));

            if (jn["s"] != null)
                beatmapObject.shape = jn["s"].AsInt;

            if (jn["shape"] != null)
                beatmapObject.shape = jn["shape"].AsInt;

            if (jn["so"] != null)
                beatmapObject.shapeOption = jn["so"].AsInt;

            if (jn["text"] != null)
                beatmapObject.text = ((string)jn["text"]).Replace("{{colon}}", ":");

            if (jn["ak"] != null)
                beatmapObject.autoKillType = jn["ak"].AsBool ? AutoKillType.LastKeyframe : AutoKillType.OldStyleNoAutokill;
            else if (jn["akt"] != null)
                beatmapObject.autoKillType = (AutoKillType)jn["akt"].AsInt;

            if (jn["ako"] != null)
                beatmapObject.autoKillOffset = jn["ako"].AsFloat;

            if (jn["o"] != null)
                beatmapObject.origin = new Vector2(jn["o"]["x"].AsFloat, jn["o"]["y"].AsFloat);

            if (jn["ed"]["locked"] != null)
                beatmapObject.editorData.locked = jn["ed"]["locked"].AsBool;

            if (jn["ed"]["shrink"] != null)
                beatmapObject.editorData.collapse = jn["ed"]["shrink"].AsBool;

            if (jn["ed"]["bin"] != null)
                beatmapObject.editorData.Bin = jn["ed"]["bin"].AsInt;

            if (jn["ed"]["layer"] != null)
                beatmapObject.editorData.layer = Mathf.Clamp(jn["ed"]["layer"].AsInt, 0, int.MaxValue);

            for (int i = 0; i < jn["modifiers"].Count; i++)
            {
                var modifier = Modifier<BeatmapObject>.Parse(jn["modifiers"][i], beatmapObject);
                beatmapObject.modifiers.Add(modifier);
            }

            return beatmapObject;
        }

        /// <summary>
        /// Converts the current Beatmap Object to the VG format.
        /// </summary>
        /// <returns></returns>
        public JSONNode ToJSONVG()
        {
            var jn = JSON.Parse("{}");

            jn["id"] = id;
            if (!string.IsNullOrEmpty(prefabID))
                jn["pre_id"] = prefabID;

            if (!string.IsNullOrEmpty(prefabInstanceID))
                jn["pre_iid"] = prefabInstanceID;

            if (parentType != "101")
                jn["p_t"] = parentType;

            if (parentOffsets.FindIndex(x => x != 0f) != -1)
            {
                int index = 0;
                foreach (float offset in parentOffsets)
                {
                    jn["p_o"][index] = offset;
                    index++;
                }
            }

            if (!string.IsNullOrEmpty(parent))
                jn["p_id"] = parent == "CAMERA_PARENT" ? "camera" : parent;

            jn["d"] = depth;

            jn["st"] = StartTime;

            if (!string.IsNullOrEmpty(name))
                jn["n"] = name;

            jn["ot"] = (int)objectType == 4 ? 0 : (int)objectType;
            jn["ak_t"] = (int)autoKillType;
            jn["ak_o"] = autoKillOffset;

            if (shape != 0)
                jn["s"] = shape;

            if (shapeOption != 0)
                jn["so"] = shapeOption;

            if (!string.IsNullOrEmpty(text))
                jn["text"] = text;

            jn["o"]["x"] = origin.x;
            jn["o"]["y"] = origin.y;

            if (editorData.locked)
                jn["ed"]["lk"] = editorData.locked;

            if (editorData.collapse)
                jn["ed"]["co"] = editorData.collapse;

            jn["ed"]["b"] = editorData.Bin;

            jn["ed"]["l"].AsInt = Mathf.Clamp(editorData.layer, 0, 5);

            // Events
            {
                // Position
                for (int j = 0; j < events[0].Count; j++)
                {
                    var eventKeyframe = events[0][j];
                    jn["e"][0]["k"][j]["t"] = eventKeyframe.eventTime;
                    jn["e"][0]["k"][j]["ct"] = eventKeyframe.curveType.Name;

                    jn["e"][0]["k"][j]["ev"][0] = eventKeyframe.eventValues[0];
                    jn["e"][0]["k"][j]["ev"][1] = eventKeyframe.eventValues[1];

                    jn["e"][0]["k"][j]["r"] = eventKeyframe.random;

                    jn["e"][0]["k"][j]["er"][0] = eventKeyframe.eventRandomValues[0];
                    jn["e"][0]["k"][j]["er"][1] = eventKeyframe.eventRandomValues[1];
                    jn["e"][0]["k"][j]["er"][2] = eventKeyframe.eventRandomValues[2];
                }

                // Scale
                for (int j = 0; j < events[1].Count; j++)
                {
                    var eventKeyframe = events[1][j];
                    jn["e"][1]["k"][j]["t"] = eventKeyframe.eventTime;
                    jn["e"][1]["k"][j]["ct"] = eventKeyframe.curveType.Name;

                    jn["e"][1]["k"][j]["ev"][0] = eventKeyframe.eventValues[0];
                    jn["e"][1]["k"][j]["ev"][1] = eventKeyframe.eventValues[1];

                    jn["e"][1]["k"][j]["r"] = eventKeyframe.random;

                    jn["e"][1]["k"][j]["er"][0] = eventKeyframe.eventRandomValues[0];
                    jn["e"][1]["k"][j]["er"][1] = eventKeyframe.eventRandomValues[1];
                    jn["e"][1]["k"][j]["er"][2] = eventKeyframe.eventRandomValues[2];
                }

                // Rotation
                for (int j = 0; j < events[2].Count; j++)
                {
                    var eventKeyframe = events[2][j];
                    jn["e"][2]["k"][j]["t"] = eventKeyframe.eventTime;
                    jn["e"][2]["k"][j]["ct"] = eventKeyframe.curveType.Name;

                    jn["e"][2]["k"][j]["ev"][0] = eventKeyframe.eventValues[0];

                    jn["e"][2]["k"][j]["r"] = eventKeyframe.random;

                    jn["e"][2]["k"][j]["er"][0] = eventKeyframe.eventRandomValues[0];
                    jn["e"][2]["k"][j]["er"][1] = eventKeyframe.eventRandomValues[1];
                    jn["e"][2]["k"][j]["er"][2] = eventKeyframe.eventRandomValues[2];
                }

                // Color
                for (int j = 0; j < events[3].Count; j++)
                {
                    var eventKeyframe = events[3][j];
                    jn["e"][3]["k"][j]["t"] = eventKeyframe.eventTime;
                    jn["e"][3]["k"][j]["ct"] = eventKeyframe.curveType.Name;

                    jn["e"][3]["k"][j]["ev"][0] = Mathf.Clamp(eventKeyframe.eventValues[0], 0, 8);
                    jn["e"][3]["k"][j]["ev"][1] = (-eventKeyframe.eventValues[1] + 1f) * 100f;
                }
            }

            return jn;
        }

        /// <summary>
        /// Converts the current Beatmap Object to the LS format.
        /// </summary>
        /// <returns>Returns a JSONNode.</returns>
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["id"] = id;
            if (!string.IsNullOrEmpty(prefabID))
                jn["pid"] = prefabID;

            if (!string.IsNullOrEmpty(prefabInstanceID))
                jn["piid"] = prefabInstanceID;

            if (parentType != "101")
                jn["pt"] = parentType;

            if (parentOffsets.Any(x => x != 0f))
            {
                for (int i = 0; i < parentOffsets.Count; i++)
                    jn["po"][i] = parentOffsets[i].ToString();
            }
            
            if (parentAdditive != "000")
                jn["pa"] = parentAdditive;

            if (parallaxSettings.Any(x => x != 1f))
            {
                for (int i = 0; i < parallaxSettings.Length; i++)
                    jn["ps"][i] = parallaxSettings[i].ToString();
            }

            if (!string.IsNullOrEmpty(parent))
                jn["p"] = parent;

            jn["d"] = depth.ToString();
            if (background)
                jn["rdt"] = "1";
            if (opacityCollision)
                jn["opcol"] = opacityCollision.ToString();
            if (ignoreLifespan)
                jn["iglif"] = ignoreLifespan.ToString();
            if (desync && !string.IsNullOrEmpty(parent))
                jn["desync"] = desync.ToString();

            if (LDM)
                jn["ldm"] = LDM.ToString();

            jn["st"] = StartTime.ToString();

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name.Replace(":", "{{colon}}");

            jn["ot"] = (int)objectType;
            jn["akt"] = (int)autoKillType;
            jn["ako"] = autoKillOffset;

            if (shape != 0)
                jn["shape"] = shape.ToString();

            if (shapeOption != 0)
                jn["so"] = shapeOption.ToString();

            if (!string.IsNullOrEmpty(text))
                jn["text"] = text.Replace(":", "{{colon}}");

            if (tags != null && tags.Count > 0)
                for (int i = 0; i < tags.Count; i++)
                    jn["tags"][i] = tags[i].Replace(":", "{{colon}}");

            if (origin.x != 0f || origin.y != 0f)
            {
                jn["o"]["x"] = origin.x.ToString();
                jn["o"]["y"] = origin.y.ToString();
            }

            if (editorData.locked)
                jn["ed"]["locked"] = editorData.locked.ToString();
            if (editorData.collapse)
                jn["ed"]["shrink"] = editorData.collapse.ToString();

            jn["ed"]["bin"] = editorData.Bin.ToString();
            jn["ed"]["layer"] = editorData.layer.ToString();

            for (int i = 0; i < events[0].Count; i++)
                jn["events"]["pos"][i] = ((EventKeyframe)events[0][i]).ToJSON();
            for (int i = 0; i < events[1].Count; i++)
                jn["events"]["sca"][i] = ((EventKeyframe)events[1][i]).ToJSON();
            for (int i = 0; i < events[2].Count; i++)
                jn["events"]["rot"][i] = ((EventKeyframe)events[2][i]).ToJSON(true);
            for (int i = 0; i < events[3].Count; i++)
                jn["events"]["col"][i] = ((EventKeyframe)events[3][i]).ToJSON();

            for (int i = 0; i < modifiers.Count; i++)
                jn["modifiers"][i] = modifiers[i].ToJSON();

            return jn;
        }

        /// <summary>
        /// Sets the parent additive value depending on the index.
        /// </summary>
        /// <param name="_index">The index to assign to.</param>
        /// <param name="_new">The new value to set.</param>
        public void SetParentAdditive(int _index, bool _new)
        {
            var stringBuilder = new StringBuilder(parentAdditive);
            stringBuilder[_index] = _new ? '1' : '0';
            parentAdditive = stringBuilder.ToString();
            CoreHelper.Log($"Set Parent Additive: {parentAdditive}");
        }

        public void SetAutokillToScale(List<BaseBeatmapObject> beatmapObjects)
        {
            try
            {
                var parent = this.parent;
                var beatmapObject = this;

                if (beatmapObject.events != null && beatmapObject.events.Count > 1 && beatmapObject.events[1].Last().eventTime < GetObjectLifeLength(_oldStyle: true) &&
                    (beatmapObject.events[1].Last().eventValues[0] == 0f || beatmapObject.events[1].Last().eventValues[1] == 0f ||
                    beatmapObject.events[1].Last().eventValues[0] == 0.001f || beatmapObject.events[1].Last().eventValues[1] == 0.001f) &&
                    beatmapObject.parentType[1] == '1')
                {
                    autoKillType = AutoKillType.SongTime;
                    autoKillOffset = beatmapObject.StartTime + beatmapObject.events[1].Last().eventTime;
                    return;
                }

                while (!string.IsNullOrEmpty(parent) && beatmapObjects.Any(x => x.id == parent))
                {
                    beatmapObject = (BeatmapObject)beatmapObjects.Find(x => x.id == parent);
                    parent = beatmapObject.parent;

                    if (beatmapObject.events != null && beatmapObject.events.Count > 1 && beatmapObject.events[1].Last().eventTime < GetObjectLifeLength(_oldStyle: true) &&
                        (beatmapObject.events[1].Last().eventValues[0] == 0f || beatmapObject.events[1].Last().eventValues[1] == 0f ||
                        beatmapObject.events[1].Last().eventValues[0] == 0.001f || beatmapObject.events[1].Last().eventValues[1] == 0.001f) &&
                        beatmapObject.parentType[1] == '1')
                    {
                        autoKillType = AutoKillType.SongTime;
                        autoKillOffset = beatmapObject.StartTime + beatmapObject.events[1].Last().eventTime;
                        break;
                    }
                }
            }
            catch
            {

            }
        }

        #endregion

        #region Operators

        public static implicit operator bool(BeatmapObject exists) => exists != null;

        public override bool Equals(object obj) => obj is BeatmapObject && id == (obj as BeatmapObject).id;

        public override string ToString() => id;

        public override int GetHashCode() => base.GetHashCode();

        #endregion
    }
}
