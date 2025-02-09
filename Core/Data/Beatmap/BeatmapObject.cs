using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Managers;
using ILMath;
using LSFunctions;
using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class BeatmapObject : BaseBeatmapObject
    {
        public BeatmapObject() : base()
        {
            editorData = new ObjectEditorData();
        }

        public BeatmapObject(float startTime, List<List<BaseEventKeyframe>> eventKeyframes) : this(true, startTime, string.Empty, 0, string.Empty, eventKeyframes)
        {

        }

        public BeatmapObject(bool active, float startTime, string name, int shape, string text, List<List<BaseEventKeyframe>> eventKeyframes) : base(active, startTime, name, shape, text, eventKeyframes)
        {
            editorData = new ObjectEditorData();
        }

        public BeatmapObject(float startTime) : base(startTime)
        {
            id = LSText.randomString(16);
            editorData = new ObjectEditorData();
        }

        #region Values

        string uniqueID;
        /// <summary>
        /// Unique ID used to identify different Beatmap Objects.
        /// </summary>
        public string UniqueID
        {
            get
            {
                if (string.IsNullOrEmpty(uniqueID))
                    uniqueID = LSText.randomNumString(16);
                return uniqueID;
            }
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
        /// Settings for the custom polygon shape.
        /// </summary>
        public PolygonShape polygonShapeSettings = new PolygonShape();

        /// <summary>
        /// Type of gradient the object should render as. Does not support text, image and player objects.
        /// </summary>
        public GradientType gradientType;

        public enum GradientType
        {
            /// <summary>
            /// The regular render method.
            /// </summary>
            Normal,
            /// <summary>
            /// Renders a linear gradient going from right to left.
            /// </summary>
            RightLinear,
            /// <summary>
            /// Renders a linear gradient going from left to right.
            /// </summary>
            LeftLinear,
            /// <summary>
            /// Renders a radial gradient going from out to in.
            /// </summary>
            OutInRadial,
            /// <summary>
            /// Renders a radial gradient going from in to out.
            /// </summary>
            InOutRadial
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
        /// If the order of triggers and actions matter.
        /// </summary>
        public bool orderModifiers = false;

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
        public SelectObject selector;

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

        public new ObjectEditorData editorData;

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

        #endregion

        #region Constants

        public const string CAMERA_PARENT = "CAMERA_PARENT";

        #endregion

        #region Properties

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
                var l = SpawnDuration;
                return time >= st && (time <= l + st && akt != AutoKillType.OldStyleNoAutokill && akt != AutoKillType.SongTime || akt == AutoKillType.OldStyleNoAutokill || time < ako && akt == AutoKillType.SongTime);
            }
        }

        /// <summary>
        /// Gets the total amount of keyframes the object has.
        /// </summary>
        public int KeyframeCount => events.Sum(x => x.Count);

        /// <summary>
        /// Total length of the objects' sequence.
        /// </summary>
        public float Length => events.Max(x => x.Max(x => x.eventTime));

        /// <summary>
        /// Gets the total time the object is alive for.
        /// </summary>
        public float SpawnDuration => GetObjectLifeLength(0.0f, true);

        /// <summary>
        /// Parent of the beatmap object.
        /// </summary>
        public BeatmapObject Parent => GameData.Current.beatmapObjects.Find(x => x.id == parent);

        /// <summary>
        /// Gets the prefab reference.
        /// </summary>
        public Prefab Prefab => GameData.Current.prefabs.Find(x => x.ID == prefabID);

        /// <summary>
        /// Gets the prefab object reference.
        /// </summary>
        public PrefabObject PrefabObject => GameData.Current.prefabObjects.Find(x => x.ID == prefabInstanceID);

        /// <summary>
        /// Type of the shape.
        /// </summary>
        public ShapeType ShapeType => (ShapeType)shape;

        #endregion

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
                editorData = ObjectEditorData.DeepCopy(orig.editorData),
                fromPrefab = orig.fromPrefab,
                objectType = orig.objectType,
                origin = orig.origin,
                prefabID = orig.prefabID,
                prefabInstanceID = orig.prefabInstanceID,
                gradientType = orig.gradientType,
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
                orderModifiers = orig.orderModifiers,
                opacityCollision = orig.opacityCollision,
                desync = orig.desync
            };

            for (int i = 0; i < beatmapObject.events.Count; i++)
                beatmapObject.events[i].AddRange(orig.events[i].Select(x => EventKeyframe.DeepCopy((EventKeyframe)x)));

            beatmapObject.modifiers = new List<Modifier<BeatmapObject>>();
            beatmapObject.modifiers = orig.modifiers.Count > 0 ? orig.modifiers.Select(x => Modifier<BeatmapObject>.DeepCopy(x, beatmapObject)).ToList() : new List<Modifier<BeatmapObject>>();
            return beatmapObject;
        }

        /// <summary>
        /// Parses a Beatmap Object from VG to formatted JSON.
        /// </summary>
        /// <param name="jn">VG JSON.</param>
        /// <returns>Returns a parsed Beatmap Object.</returns>
        public static BeatmapObject ParseVG(JSONNode jn, Version version = default)
        {
            var beatmapObject = new BeatmapObject();

            var events = new List<List<BaseEventKeyframe>>();
            events.Add(new List<BaseEventKeyframe>());
            events.Add(new List<BaseEventKeyframe>());
            events.Add(new List<BaseEventKeyframe>());
            events.Add(new List<BaseEventKeyframe>());

            var isCameraParented = jn["p_id"] == "camera";

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

                        if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                            eventKeyframe.curveType = anim;

                        eventKeyframe.SetEventValues(
                            kfjn["ev"][0].AsFloat,
                            kfjn["ev"][1].AsFloat,
                            isCameraParented ? -10f : 0f);

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

                        if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                            eventKeyframe.curveType = anim;

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

                        if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                            eventKeyframe.curveType = anim;

                        eventKeyframe.SetEventValues(
                            kfjn["ev"][0].AsFloat);

                        eventKeyframe.random = kfjn["r"].AsInt;

                        eventKeyframe.SetEventRandomValues(
                            kfjn["er"][0].AsFloat,
                            kfjn["er"][1].AsFloat,
                            kfjn["er"][2].AsFloat);

                        eventKeyframe.relative = true;
                        if (version >= new Version(ProjectArrhythmia.Versions.FIXED_ROTATION_SHAKE) && kfjn["ev"].Count > 1 && !kfjn["ev"][1].IsNull && kfjn["ev"][1].AsFloat == 1)
                            eventKeyframe.relative = false;

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

                        if (kfjn["ct"] != null && DataManager.inst.AnimationListDictionaryStr.TryGetValue(kfjn["ct"], out DataManager.LSAnimation anim))
                            eventKeyframe.curveType = anim;

                        // 0 = start color slot
                        // 1 = start opacity
                        // 2 = start hue
                        // 3 = start saturation
                        // 4 = start value
                        // 5 = end color slot
                        // 6 = end opacity
                        // 7 = end hue
                        // 8 = end saturation
                        // 9 = end value
                        eventKeyframe.SetEventValues(
                            kfjn["ev"][0].AsFloat,
                            kfjn["ev"].Count <= 1 ? 0f : (-(kfjn["ev"][1].AsFloat / 100f) + 1f),
                            0f,
                            0f,
                            0f,
                            kfjn["ev"].Count <= 2 ? 0f : kfjn["ev"][2].AsFloat,
                            10f,
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

            beatmapObject.id = jn["id"] ?? LSText.randomString(16);

            beatmapObject.opacityCollision = true;

            if (jn["pre_iid"] != null)
                beatmapObject.prefabInstanceID = jn["pre_iid"];

            if (jn["pre_id"] != null)
                beatmapObject.prefabID = jn["pre_id"];

            if (jn["p_id"] != null)
                beatmapObject.parent = isCameraParented ? CAMERA_PARENT : jn["p_id"];

            if (!isCameraParented && jn["p_t"] != null)
                beatmapObject.parentType = jn["p_t"];
            if (isCameraParented)
                beatmapObject.parentType = "111";

            if (jn["p_o"] != null && jn["p_id"] != "camera")
                beatmapObject.parentOffsets = jn["p_o"].AsArray.Children.Select(x => x.AsFloat).ToList();
            else if (jn["p_id"] == "camera")
                beatmapObject.parentOffsets = new List<float> { 0f, 0f, 0f };

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
                beatmapObject.Depth = jn["d"].AsInt;
            else
                beatmapObject.Depth = version == new Version(ProjectArrhythmia.Versions.DEPTH_DEFAULT_CHANGED) ? 0 : 20; // fixes default depth not being correct

            if (jn["s"] != null)
                beatmapObject.shape = jn["s"].AsInt;

            if (jn["so"] != null)
                beatmapObject.shapeOption = jn["so"].AsInt;

            if (jn["csp"] != null)
                beatmapObject.polygonShapeSettings = PolygonShape.Parse(jn["csp"]);

            if (jn["gt"] != null)
                beatmapObject.gradientType = (GradientType)jn["gt"].AsInt;

            if (jn["text"] != null)
                beatmapObject.text = jn["text"];

            if (jn["ak_t"] != null)
                beatmapObject.autoKillType = (AutoKillType)jn["ak_t"].AsInt;

            if (jn["ak_o"] != null)
                beatmapObject.autoKillOffset = jn["ak_o"].AsFloat;

            if (jn["o"] != null)
                beatmapObject.origin = new Vector2(jn["o"]["x"].AsFloat, jn["o"]["y"].AsFloat);

            if (jn["ed"] != null)
                beatmapObject.editorData = ObjectEditorData.ParseVG(jn["ed"]);

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

                    // if gradient objects are implemented
                    // x - 0 = start color slot
                    // y - 1 = start opacity
                    // z - 2 = start hue
                    // x2 - 3 = start saturation
                    // y2 - 4 = start value
                    // z2 - 5 = end color slot
                    // x3 = end opacity
                    // y3 = end hue
                    // z3 = end saturation
                    // x4 = end value

                    if (kfjn["z2"] != null) // check for gradient values
                        eventKeyframe.SetEventValues(
                        kfjn["x"].AsFloat,
                        kfjn["y"].AsFloat,
                        kfjn["z"].AsFloat,
                        kfjn["x2"].AsFloat,
                        kfjn["y2"].AsFloat,
                        kfjn["z2"].AsFloat,
                        kfjn["x3"].AsFloat,
                        kfjn["y3"].AsFloat,
                        kfjn["z3"].AsFloat,
                        kfjn["x4"].AsFloat);
                    else // no gradient values
                        eventKeyframe.SetEventValues(
                            kfjn["x"].AsFloat,
                            kfjn["y"].AsFloat,
                            kfjn["z"].AsFloat,
                            kfjn["x2"].AsFloat,
                            kfjn["y2"].AsFloat,
                            0f,
                            0f,
                            0f,
                            0f,
                            0f);

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
                beatmapObject.parentOffsets = jn["po"].AsArray.Children.Select(x => x.AsFloat).ToList();

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
                beatmapObject.Depth = jn["d"].AsInt;

            if (jn["rdt"] != null)
                beatmapObject.background = jn["rdt"].AsInt == 1;

            if (jn["opcol"] != null)
                beatmapObject.opacityCollision = jn["opcol"].AsBool;

            if (jn["iglif"] != null)
                beatmapObject.ignoreLifespan = jn["iglif"].AsBool;

            if (jn["ordmod"] != null)
                beatmapObject.orderModifiers = jn["ordmod"].AsBool;

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

            if (jn["csp"] != null)
                beatmapObject.polygonShapeSettings = PolygonShape.Parse(jn["csp"]);

            if (jn["gt"] != null)
                beatmapObject.gradientType = (GradientType)jn["gt"].AsInt;

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

            if (jn["ed"] != null)
                beatmapObject.editorData = ObjectEditorData.Parse(jn["ed"]);

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
                jn["p_id"] = parent == CAMERA_PARENT ? "camera" : parent;

            jn["d"] = Depth;

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

            if (polygonShapeSettings != null)
                jn["csp"] = polygonShapeSettings.ToJSON();

            if (gradientType != GradientType.Normal)
                jn["gt"] = ((int)gradientType).ToString();

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
                    var eventKeyframe = events[2][j] as EventKeyframe;
                    jn["e"][2]["k"][j]["t"] = eventKeyframe.eventTime;
                    jn["e"][2]["k"][j]["ct"] = eventKeyframe.curveType.Name;

                    jn["e"][2]["k"][j]["ev"][0] = eventKeyframe.eventValues[0];

                    if (!eventKeyframe.relative)
                        jn["e"][2]["k"][j]["ev"][1] = 1.0f;

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
                    if (eventKeyframe.eventValues.Length > 5)
                        jn["e"][3]["k"][j]["ev"][2] = Mathf.Clamp(eventKeyframe.eventValues[5], 0, 8);
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

            if (Depth != 15)
                jn["d"] = Depth.ToString();
            if (background)
                jn["rdt"] = "1";
            if (opacityCollision)
                jn["opcol"] = opacityCollision.ToString();
            if (ignoreLifespan)
                jn["iglif"] = ignoreLifespan.ToString();
            if (orderModifiers)
                jn["ordmod"] = orderModifiers.ToString();
            if (desync && !string.IsNullOrEmpty(parent))
                jn["desync"] = desync.ToString();

            if (LDM)
                jn["ldm"] = LDM.ToString();

            jn["st"] = StartTime.ToString();

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;

            jn["ot"] = (int)objectType;
            jn["akt"] = (int)autoKillType;
            jn["ako"] = autoKillOffset;

            if (gradientType != GradientType.Normal)
                jn["gt"] = ((int)gradientType).ToString();

            if (shape != 0)
                jn["shape"] = shape.ToString();

            if (shapeOption != 0)
                jn["so"] = shapeOption.ToString();

            if (ShapeType == ShapeType.Polygon && polygonShapeSettings != null)
                jn["csp"] = polygonShapeSettings.ToJSON();

            if (!string.IsNullOrEmpty(text))
                jn["text"] = text;

            if (tags != null && tags.Count > 0)
                for (int i = 0; i < tags.Count; i++)
                    jn["tags"][i] = tags[i];

            if (origin.x != 0f || origin.y != 0f)
                jn["o"] = origin.ToJSON();

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
                jn["events"]["col"][i] = ((EventKeyframe)events[3][i]).ToJSON(maxValuesToSave: gradientType != GradientType.Normal ? -1 : 5);

            for (int i = 0; i < modifiers.Count; i++)
                jn["modifiers"][i] = modifiers[i].ToJSON();

            return jn;
        }

        /// <summary>
        /// Gets the objects' lifetime based on its autokill type and offset.
        /// </summary>
        /// <param name="offset">Offset to apply to lifetime.</param>
        /// <param name="oldStyle">If the autokill length should be considered.</param>
        /// <param name="collapse">If the length should be collapsed.</param>
        /// <returns>Returns the lifetime of the object.</returns>
        public new float GetObjectLifeLength(float offset = 0f, bool oldStyle = false, bool collapse = false) => collapse && editorData.collapse ? 0.2f : autoKillType switch
        {
            AutoKillType.OldStyleNoAutokill => oldStyle ? AudioManager.inst.CurrentAudioSource.clip.length - startTime : Length + offset,
            AutoKillType.LastKeyframe => Length + offset,
            AutoKillType.LastKeyframeOffset => Length + autoKillOffset + offset,
            AutoKillType.FixedTime => autoKillOffset,
            AutoKillType.SongTime => (startTime >= autoKillOffset) ? 0.1f : (autoKillOffset - startTime),
            _ => 0f,
        };

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

        public void SetAutokillToScale(List<BeatmapObject> beatmapObjects)
        {
            try
            {
                var parent = this.parent;
                var beatmapObject = this;
                var spawnDuration = SpawnDuration;

                if (beatmapObject.events != null && beatmapObject.events.Count > 1 && beatmapObject.events[1].Last().eventTime < spawnDuration &&
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
                    beatmapObject = beatmapObjects.Find(x => x.id == parent);
                    parent = beatmapObject.parent;

                    if (beatmapObject.events != null && beatmapObject.events.Count > 1 && beatmapObject.events[1].Last().eventTime < spawnDuration &&
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

        public bool TryGetPrefabObject(out PrefabObject result)
        {
            if (GameData.Current.prefabObjects.TryFind(x => x.ID == prefabInstanceID, out PrefabObject prefabObject))
            {
                result = prefabObject;
                return true;
            }
            result = null;
            return false;
        }

        #region Prefab Reference

        /// <summary>
        /// Removes the Prefab and Prefab Object ID references.
        /// </summary>
        public void RemovePrefabReference()
        {
            prefabID = "";
            prefabInstanceID = "";
        }

        /// <summary>
        /// Sets the Prefab and Prefab Object ID references from a Prefab Object.
        /// </summary>
        /// <param name="prefabObject">Prefab Object reference.</param>
        public void SetPrefabReference(PrefabObject prefabObject)
        {
            prefabID = prefabObject.prefabID;
            prefabInstanceID = prefabObject.ID;
        }

        /// <summary>
        /// Sets the Prefab and Prefab Object ID references from another object.
        /// </summary>
        /// <param name="beatmapObject">Object reference.</param>
        public void SetPrefabReference(BeatmapObject beatmapObject)
        {
            prefabID = beatmapObject.prefabID;
            prefabInstanceID = beatmapObject.prefabInstanceID;
        }

        #endregion

        #region Custom Interpolation

        public Vector3 GetTransformOffset(int type) => type switch
        {
            0 => positionOffset,
            1 => scaleOffset,
            _ => rotationOffset,
        };

        public void SetTransform(int toType, Vector3 value)
        {
            switch (toType)
            {
                case 0:
                    {
                        positionOffset = value;
                        break;
                    }
                case 1:
                    {
                        scaleOffset = value;
                        break;
                    }
                case 2:
                    {
                        rotationOffset = value;
                        break;
                    }
            }
        }

        public void SetTransform(int toType, int toAxis, float value)
        {
            switch (toType)
            {
                case 0:
                    {
                        positionOffset[toAxis] = value;
                        break;
                    }
                case 1:
                    {
                        scaleOffset[toAxis] = value;
                        break;
                    }
                case 2:
                    {
                        rotationOffset[toAxis] = value;
                        break;
                    }
            }
        }

        public float Interpolate(int type, int valueIndex) => Interpolate(type, valueIndex, Updater.CurrentTime - StartTime);
        
        public float Interpolate(int type, int valueIndex, float time)
        {
            var list = events[type].OrderBy(x => x.eventTime).ToList();

            var nextKFIndex = list.FindIndex(x => x.eventTime > time);

            if (nextKFIndex < 0)
                nextKFIndex = list.Count - 1;

            var prevKFIndex = nextKFIndex - 1;
            if (prevKFIndex < 0)
                prevKFIndex = 0;

            var nextKF = list[nextKFIndex] as EventKeyframe;
            var prevKF = list[prevKFIndex] as EventKeyframe;

            type = Mathf.Clamp(type, 0, list.Count);
            valueIndex = Mathf.Clamp(valueIndex, 0, list[0].eventValues.Length);

            if (prevKF.eventValues.Length <= valueIndex)
                return 0f;

            var total = 0f;
            var prevtotal = 0f;
            for (int k = 0; k < nextKFIndex; k++)
            {
                if (((EventKeyframe)list[k + 1]).relative)
                    total += list[k].eventValues[valueIndex];
                else
                    total = 0f;

                if (((EventKeyframe)list[k]).relative)
                    prevtotal += list[k].eventValues[valueIndex];
                else
                    prevtotal = 0f;
            }

            var next = nextKF.relative ? total + nextKF.eventValues[valueIndex] : nextKF.eventValues[valueIndex];
            var prev = prevKF.relative || nextKF.relative ? prevtotal : prevKF.eventValues[valueIndex];

            bool isLerper = type != 3 || valueIndex != 0;

            if (float.IsNaN(prev) || !isLerper)
                prev = 0f;

            if (float.IsNaN(next))
                next = 0f;

            if (!isLerper)
                next = 1f;

            if (prevKFIndex == nextKFIndex)
                return next;

            var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curveType.Name)(RTMath.InverseLerp(prevKF.eventTime, nextKF.eventTime, Mathf.Clamp(time, 0f, nextKF.eventTime))));

            if (prevKFIndex == nextKFIndex)
                x = next;

            if (float.IsNaN(x) || float.IsInfinity(x))
                x = next;

            return x;
        }

        public float InterpolateChain(int type, int valueIndex, float time)
        {
            float result = 0f;

            var parents = GetParentChain();
            parents.Reverse();

            for (int i = 0; i < parents.Count; i++)
                result += parents[i].Interpolate(type, valueIndex, time);

            return result;
        }

        /// <summary>
        /// Gets the accurate object position regardless of whether it's empty or not. (does not include homing nor random)
        /// </summary>
        /// <param name="includeDepth">If depth should be considered.</param>
        /// <returns>Returns an accurate object position.</returns>
        public Vector3 InterpolateChainPosition(bool includeDepth = false, bool includeOffsets = true, bool includeSelf = true) => InterpolateChainPosition(Updater.CurrentTime - StartTime, includeDepth, includeOffsets, includeSelf);

        /// <summary>
        /// Gets the accurate object position regardless of whether it's empty or not. (does not include homing nor random)
        /// </summary>
        /// <param name="time">The time to interpolate.</param>
        /// <param name="includeDepth">If depth should be considered.</param>
        /// <returns>Returns an accurate object position.</returns>
        public Vector3 InterpolateChainPosition(float time, bool includeDepth = false, bool includeOffsets = true, bool includeSelf = true)
        {
            Vector3 result = Vector3.zero;

            var parents = GetParentChain();

            bool animatePosition = true;
            bool animateScale = true;
            bool animateRotation = true;

            for (int i = 0; i < parents.Count; i++)
            {
                if (!includeSelf && i == 0)
                    continue;

                var parent = parents[i];

                if (animateScale)
                    result = RTMath.Scale(result, new Vector2(parent.Interpolate(1, 0, time), parent.Interpolate(1, 1, time)));

                if (animateRotation)
                    result = RTMath.Rotate(result, parent.Interpolate(2, 0, time));

                if (animatePosition)
                    result = RTMath.Move(result, new Vector3(parent.Interpolate(0, 0, time), parent.Interpolate(0, 1, time), parent.Interpolate(0, 2, time)));

                animatePosition = parent.GetParentType(0);
                animateScale = parent.GetParentType(1);
                animateRotation = parent.GetParentType(2);

                if (includeOffsets)
                    result += parent.positionOffset;
            }

            result.z += includeDepth ? Depth : 0f;
            return result;
        }

        public Vector2 InterpolateChainScale(bool includeOffsets = true, bool includeSelf = true) => InterpolateChainScale(Updater.CurrentTime - StartTime, includeOffsets, includeSelf);

        public Vector2 InterpolateChainScale(float time, bool includeOffsets = true, bool includeSelf = true)
        {
            Vector2 result = Vector2.one;

            var parents = GetParentChain();

            bool animateScale = true;

            for (int i = 0; i < parents.Count; i++)
            {
                if (!includeSelf && i == 0)
                    continue;

                var parent = parents[i];

                if (animateScale)
                    result = RTMath.Scale(result, new Vector2(parent.Interpolate(1, 0, time), parent.Interpolate(1, 1, time)));

                animateScale = parent.GetParentType(1);

                if (includeOffsets)
                    result = new Vector2(result.x + parent.scaleOffset.x, result.y + parent.scaleOffset.y);
            }

            return result;
        }

        public float InterpolateChainRotation(bool includeOffsets = true, bool includeSelf = true) => InterpolateChainRotation(Updater.CurrentTime - StartTime, includeOffsets, includeSelf);

        public float InterpolateChainRotation(float time, bool includeOffsets = true, bool includeSelf = true)
        {
            float result = 0f;

            var parents = GetParentChain();

            bool animateRotation = true;

            for (int i = 0; i < parents.Count; i++)
            {
                if (!includeSelf && i == 0)
                    continue;

                var parent = parents[i];

                if (animateRotation)
                    result += parent.Interpolate(2, 0, time);

                animateRotation = parent.GetParentType(2);

                if (includeOffsets)
                    result += parent.rotationOffset.z;
            }

            return result;
        }

        public ObjectAnimationResult InterpolateChain(bool includeDepth = false, bool includeOffsets = true, bool includeSelf = true) => InterpolateChain(Updater.CurrentTime - StartTime, includeDepth, includeOffsets, includeSelf);

        public ObjectAnimationResult InterpolateChain(float time, bool includeDepth = false, bool includeOffsets = true, bool includeSelf = true)
        {
            var result = ObjectAnimationResult.Default;

            var parents = GetParentChain();

            bool animatePosition = true;
            bool animateScale = true;
            bool animateRotation = true;

            for (int i = 0; i < parents.Count; i++)
            {
                if (!includeSelf && i == 0)
                    continue;

                var parent = parents[i];

                if (animateScale)
                {
                    var scale = new Vector2(parent.Interpolate(1, 0, time), parent.Interpolate(1, 1, time));
                    result.position = RTMath.Scale(result.position, scale);
                    result.scale = RTMath.Scale(result.scale, scale);
                }

                if (animateRotation)
                {
                    var rotation = parent.Interpolate(2, 0, time);
                    result.position = RTMath.Rotate(result.position, rotation);
                    result.rotation += parent.Interpolate(2, 0, time);
                }

                if (animatePosition)
                    result.position = RTMath.Move(result.position, new Vector3(parent.Interpolate(0, 0, time), parent.Interpolate(0, 1, time), parent.Interpolate(0, 2, time)));

                animatePosition = parent.GetParentType(0);
                animateScale = parent.GetParentType(1);
                animateRotation = parent.GetParentType(2);

                if (includeOffsets)
                {
                    result.position += parent.positionOffset;
                    result.scale = new Vector2(result.scale.x + parent.scaleOffset.x, result.scale.y + parent.scaleOffset.y);
                    result.rotation += parent.rotationOffset.z;
                }
            }

            result.position.z += includeDepth ? Depth : 0f;

            return result;
        }

        public struct ObjectAnimationResult
        {
            public static ObjectAnimationResult Default => new ObjectAnimationResult(Vector3.zero, Vector2.one, 0f);

            public ObjectAnimationResult(Vector3 position, Vector2 scale, float rotation)
            {
                this.position = position;
                this.scale = scale;
                this.rotation = rotation;
            }

            public Vector3 position;
            public Vector2 scale;
            public float rotation;
        }

        #endregion

        #region Evaluation

        public Dictionary<string, float> GetObjectVariables()
        {
            var variables = new Dictionary<string, float>();
            SetObjectVariables(variables);
            return variables;
        }
        
        public Dictionary<string, float> GetOtherObjectVariables()
        {
            var variables = new Dictionary<string, float>();
            SetOtherObjectVariables(variables);
            return variables;
        }

        public Dictionary<string, MathFunction> GetObjectFunctions()
        {
            var functions = new Dictionary<string, MathFunction>();
            SetObjectFunctions(functions);
            return functions;
        }

        public void SetOtherObjectVariables(Dictionary<string, float> variables)
        {
            variables["otherIntVariable"] = integerVariable;

            variables["otherPositionOffsetX"] = positionOffset.x;
            variables["otherPositionOffsetY"] = positionOffset.y;
            variables["otherPositionOffsetZ"] = positionOffset.z;

            variables["otherScaleOffsetX"] = scaleOffset.x;
            variables["otherScaleOffsetY"] = scaleOffset.y;
            variables["otherScaleOffsetZ"] = scaleOffset.z;

            variables["otherRotationOffsetX"] = rotationOffset.x;
            variables["otherRotationOffsetY"] = rotationOffset.y;
            variables["otherRotationOffsetZ"] = rotationOffset.z;

            if (Updater.TryGetObject(this, out Optimization.Objects.LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
            {
                var transform = levelObject.visualObject.GameObject.transform;

                variables["otherVisualPosX"] = transform.position.x;
                variables["otherVisualPosY"] = transform.position.y;
                variables["otherVisualPosZ"] = transform.position.z;

                variables["otherVisualScaX"] = transform.lossyScale.x;
                variables["otherVisualScaY"] = transform.lossyScale.y;
                variables["otherVisualScaZ"] = transform.lossyScale.z;

                variables["otherVisualRotX"] = transform.rotation.eulerAngles.x;
                variables["otherVisualRotY"] = transform.rotation.eulerAngles.y;
                variables["otherVisualRotZ"] = transform.rotation.eulerAngles.z;
            }
        }

        public void SetObjectVariables(Dictionary<string, float> variables)
        {
            variables["intVariable"] = integerVariable;

            variables["positionOffsetX"] = positionOffset.x;
            variables["positionOffsetY"] = positionOffset.y;
            variables["positionOffsetZ"] = positionOffset.z;

            variables["scaleOffsetX"] = scaleOffset.x;
            variables["scaleOffsetY"] = scaleOffset.y;
            variables["scaleOffsetZ"] = scaleOffset.z;

            variables["rotationOffsetX"] = rotationOffset.x;
            variables["rotationOffsetY"] = rotationOffset.y;
            variables["rotationOffsetZ"] = rotationOffset.z;

            if (Updater.TryGetObject(this, out Optimization.Objects.LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
            {
                var transform = levelObject.visualObject.GameObject.transform;

                variables["visualPosX"] = transform.position.x;
                variables["visualPosY"] = transform.position.y;
                variables["visualPosZ"] = transform.position.z;

                variables["visualScaX"] = transform.lossyScale.x;
                variables["visualScaY"] = transform.lossyScale.y;
                variables["visualScaZ"] = transform.lossyScale.z;

                variables["visualRotX"] = transform.rotation.eulerAngles.x;
                variables["visualRotY"] = transform.rotation.eulerAngles.y;
                variables["visualRotZ"] = transform.rotation.eulerAngles.z;
            }
        }

        public void SetObjectFunctions(Dictionary<string, MathFunction> functions)
        {
            functions["interpolateChainPosX"] = parameters =>
            {
                return InterpolateChainPosition((float)parameters[0], parameters[1] == 1, parameters[2] == 1, parameters[3] == 1).x;
            };
            functions["interpolateChainPosY"] = parameters =>
            {
                return InterpolateChainPosition((float)parameters[0], parameters[1] == 1, parameters[2] == 1, parameters[3] == 1).y;
            };
            functions["interpolateChainPosZ"] = parameters =>
            {
                return InterpolateChainPosition((float)parameters[0], parameters[1] == 1, parameters[2] == 1, parameters[3] == 1).z;
            };
            functions["interpolateChainScaX"] = parameters =>
            {
                return InterpolateChainScale((float)parameters[0], parameters[1] == 1, parameters[2] == 1).x;
            };
            functions["interpolateChainScaY"] = parameters =>
            {
                return InterpolateChainScale((float)parameters[0], parameters[1] == 1, parameters[2] == 1).y;
            };
            functions["interpolateChainRot"] = parameters =>
            {
                return InterpolateChainRotation((float)parameters[0], parameters[1] == 1, parameters[2] == 1);
            };
        }

        #endregion

        #region Parent / Child

        /// <summary>
        /// Gets the parent of the object.
        /// </summary>
        /// <returns>Returns a <see cref="BeatmapObject"/> that this object is parented to. If no object was found, returns null.</returns>
        public BeatmapObject GetParent() => GetParent(GameData.Current.beatmapObjects);

        /// <summary>
        /// Gets the parent of the object.
        /// </summary>
        /// <param name="beatmapObjects">Beatmap Object list to search for a parent.</param>
        /// <returns>Returns a <see cref="BeatmapObject"/> that this object is parented to. If no object was found, returns null.</returns>
        public BeatmapObject GetParent(List<BeatmapObject> beatmapObjects) => beatmapObjects.Find(x => x.id == parent);

        /// <summary>
        /// Iterates through the object parent chain (including the object itself).
        /// </summary>
        /// <returns>Returns a list of parents ordered by the current beatmap object to the base parent with no other parents.</returns>
        public List<BeatmapObject> GetParentChain() => GetParentChain(GameData.Current.beatmapObjects);

        /// <summary>
        /// Iterates through the object parent chain (including the object itself).
        /// </summary>
        /// <param name="beatmapObjects">Beatmap Object list to find the parent chain in.</param>
        /// <returns>Returns a list of parents ordered by the current beatmap object to the base parent with no other parents.</returns>
        public List<BeatmapObject> GetParentChain(List<BeatmapObject> beatmapObjects)
        {
            var list = new List<BeatmapObject>();

            string parent = this.parent;
            int index = beatmapObjects.FindIndex(x => x.id == parent);

            list.Add(this);
            while (index >= 0)
            {
                list.Add(beatmapObjects[index]);
                parent = beatmapObjects[index].parent;
                index = beatmapObjects.FindIndex(x => x.id == parent);
            }
            return list;
        }

        /// <summary>
        /// Iterates through the object parent chain (including the object itself).
        /// </summary>
        /// <returns>Returns a list of parents ordered by the current beatmap object to the base parent with no other parents.</returns>
        public IEnumerable<BeatmapObject> IGetParentChain() => IGetParentChain(GameData.Current.beatmapObjects);

        /// <summary>
        /// Iterates through the object parent chain (including the object itself).
        /// </summary>
        /// <param name="beatmapObjects">Beatmap Object list to find the parent chain in.</param>
        /// <returns>Returns a list of parents ordered by the current beatmap object to the base parent with no other parents.</returns>
        public IEnumerable<BeatmapObject> IGetParentChain(List<BeatmapObject> beatmapObjects)
        {
            var beatmapObject = this;
            while (beatmapObject)
            {
                yield return beatmapObject;
                beatmapObject = beatmapObject.GetParent(beatmapObjects);
            }
        }

        /// <summary>
        /// Recursively gets every child connected to the beatmap object.
        /// </summary>
        /// <returns>A full list with every child object.</returns>
        public List<BeatmapObject> GetChildTree()
        {
            var list = new List<BeatmapObject>();
            list.Add(this);
            var beatmapObjects = GameData.Current.beatmapObjects;

            string id = this.id;
            if (beatmapObjects.TryFindAll(x => x.parent == id, out List<BeatmapObject> children))
            {
                for (int i = 0; i < children.Count; i++)
                    list.AddRange(children[i].GetChildTree());
            }

            return list;
        }

        /// <summary>
        /// Gets all children parented to this object.
        /// </summary>
        /// <returns>Returns a list of the objects' children.</returns>
        public List<BeatmapObject> GetChildren() => GameData.Current.beatmapObjects.TryFindAll(x => x.parent == id, out List<BeatmapObject> children) ? children : new List<BeatmapObject>();

        public void SetParent(BeatmapObject beatmapObjectToParentTo, bool recalculate = true, bool renderParent = true) => TrySetParent(beatmapObjectToParentTo, recalculate, renderParent);

        /// <summary>
        /// Tries to set an objects' parent. If the parent the user is trying to assign an object to a child of the object, then don't set parent.
        /// </summary>
        /// <param name="beatmapObjectToParentTo">Object to try parenting to.</param>
        /// <param name="recalculate">If spawner should recalculate.</param>
        /// <returns>Returns true if the <see cref="BeatmapObject"/> was successfully parented, otherwise returns false.</returns>
        public bool TrySetParent(BeatmapObject beatmapObjectToParentTo, bool recalculate = true, bool renderParent = true)
        {
            var dictionary = new Dictionary<string, bool>();
            var beatmapObjects = GameData.Current.beatmapObjects;

            foreach (var obj in beatmapObjects)
                dictionary[obj.id] = CanParent(obj, beatmapObjects);

            dictionary[id] = false;

            var shouldParent = dictionary.TryGetValue(beatmapObjectToParentTo.id, out bool value) && value;

            if (shouldParent)
            {
                parent = beatmapObjectToParentTo.id;
                Updater.UpdateObject(this, recalculate: recalculate);

                if (renderParent)
                    ObjectEditor.inst.RenderParent(this);
            }

            return shouldParent;
        }

        /// <summary>
        /// Checks if another object can be parented to this object.
        /// </summary>
        /// <param name="obj">Object to check the parent compatibility of.</param>
        /// <param name="beatmapObjects">Beatmap objects to search through.</param>
        /// <returns>Returns true if <paramref name="obj"/> can be parented to this.</returns>
        public bool CanParent(BeatmapObject obj, List<BeatmapObject> beatmapObjects)
        {
            if (string.IsNullOrEmpty(obj.parent))
                return true;

            bool canParent = true;
            string parentID = id;

            while (!string.IsNullOrEmpty(parentID))
            {
                if (parentID == obj.parent)
                {
                    canParent = false;
                    break;
                }

                parentID = beatmapObjects.TryFind(x => x.parent == parentID, out BeatmapObject parentObj) ? parentObj.id : null;
            }

            return canParent;
        }

        #endregion

        #endregion

        #region Operators

        public static implicit operator bool(BeatmapObject exists) => exists != null;

        public override bool Equals(object obj) => obj is BeatmapObject && id == (obj as BeatmapObject).id;

        public override string ToString() => id;

        public override int GetHashCode() => base.GetHashCode();

        #endregion
    }
}
