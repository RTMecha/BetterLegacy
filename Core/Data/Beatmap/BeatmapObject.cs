using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using LSFunctions;

using ILMath;

using SimpleJSON;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents an object PA levels are made of.
    /// </summary>
    public class BeatmapObject : PAObject<BeatmapObject>, IPrefabable
    {
        public BeatmapObject() : base() { }

        public BeatmapObject(float startTime) : this() => StartTime = startTime;

        #region Values

        /// <summary>
        /// Name of the object.
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// If true, object does not render when the user has the <see cref="Configs.CoreConfig.LDM"/> setting on.
        /// </summary>
        public bool LDM { get; set; }

        /// <summary>
        /// Animation events.
        /// </summary>
        public List<List<EventKeyframe>> events = new List<List<EventKeyframe>>()
        {
            new List<EventKeyframe>(),
            new List<EventKeyframe>(),
            new List<EventKeyframe>(),
            new List<EventKeyframe>()
        };

        /// <summary>
        /// Data for the object in the editor.
        /// </summary>
        public ObjectEditorData editorData = new ObjectEditorData();

        #region Parent

        /// <summary>
        /// ID of the object to parent this to. This value is not saved and is temporarily used for swapping parents.
        /// </summary>
        public string customParent;

        string parent = string.Empty;
        /// <summary>
        /// ID of the object to parent this to.
        /// </summary>
        public string Parent
        {
            get => customParent ?? parent;
            set
            {
                customParent = null;
                parent = value;
            }
        }

        /// <summary>
        /// Parent delay values.
        /// </summary>
        public float[] parentOffsets = new float[3]
        {
            0f, // Pos
            0f, // Sca
            0f, // Rot
        };

        /// <summary>
        /// Parent toggle values.
        /// </summary>
        public string parentType = "101";

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
        /// If the object should stop following the parent chain after spawn.
        /// </summary>
        public bool desync;

        /// <summary>
        /// Temporary detatches the parent (similar to <see cref="desync"/>).
        /// </summary>
        public bool detatched;

        #endregion

        #region Timing

        float startTime;
        /// <summary>
        /// Object spawn time.
        /// </summary>
        public float StartTime
        {
            get => startTime;
            set => startTime = value;
        }

        /// <summary>
        /// Object despawn behavior.
        /// </summary>
        public AutoKillType autoKillType;

        /// <summary>
        /// Autokill time offset.
        /// </summary>
        public float autoKillOffset;

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
                return time >= st && (time <= l + st && akt != AutoKillType.NoAutokill && akt != AutoKillType.SongTime || akt == AutoKillType.NoAutokill || time < ako && akt == AutoKillType.SongTime);
            }
        }

        /// <summary>
        /// Gets the total amount of keyframes the object has.
        /// </summary>
        public int KeyframeCount => events.Sum(x => x.Count);

        /// <summary>
        /// Total length of the objects' sequence.
        /// </summary>
        public float Length => events.Max(x => x.Max(x => x.time));

        /// <summary>
        /// Gets the total time the object is alive for.
        /// </summary>
        public float SpawnDuration => GetObjectLifeLength(0.0f, true);

        #endregion

        #region Physics

        /// <summary>
        /// Object physical conditions.
        /// </summary>
        public ObjectType objectType;

        /// <summary>
        /// Object physical conditions.
        /// </summary>
        public enum ObjectType
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
            /// Has no collision but is opaque like <see cref="Normal"/>.
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
        /// If true and objects' opacity is less than 100%, disables collision. Acts the same as modern Project Arrhythmia.
        /// </summary>
        public bool opacityCollision = false;

        #endregion

        #region Transforms

        int depth = 15;
        /// <summary>
        /// The depth the visual object is rendered on.
        /// </summary>
        public int Depth
        {
            get => depth;
            set => depth = value;
        }

        /// <summary>
        /// Origin offset of the visual object.
        /// </summary>
        public Vector2 origin;

        /// <summary>
        /// The render layer of the object.
        /// </summary>
        public RenderLayerType renderLayerType;

        /// <summary>
        /// What layer an object renders on.
        /// </summary>
        public enum RenderLayerType
        {
            /// <summary>
            /// Renders in the orthographic foreground.
            /// </summary>
            Foreground,
            /// <summary>
            /// Renders in the perspective background.
            /// </summary>
            Background,
            /// <summary>
            /// Renders as UI.
            /// </summary>
            UI,
        }

        #endregion

        #region Shape

        /// <summary>
        /// Shape group.
        /// </summary>
        public int shape;

        /// <summary>
        /// Shape option.
        /// </summary>
        public int shapeOption;

        /// <summary>
        /// Text data for <see cref="ShapeType.Text"/>.
        /// </summary>
        public string text = string.Empty;

        /// <summary>
        /// If the <see cref="ShapeType.Text"/> object should align the text to the origin.
        /// </summary>
        public bool autoTextAlign;

        /// <summary>
        /// Settings for the custom polygon shape.
        /// </summary>
        public PolygonShape polygonShapeSettings = new PolygonShape();

        /// <summary>
        /// Type of gradient the object should render as. Does not support text, image and player objects.
        /// </summary>
        public GradientType gradientType;

        /// <summary>
        /// Gradient rendering type.
        /// </summary>
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
        /// Scale of the gradient.
        /// </summary>
        public float gradientScale = 1f;

        /// <summary>
        /// Rotation of the gradient.
        /// </summary>
        public float gradientRotation;

        /// <summary>
        /// Type of the shape.
        /// </summary>
        public ShapeType ShapeType
        {
            get => (ShapeType)shape;
            set => shape = (int)value;
        }

        /// <summary>
        /// If the shape has special properties: Text or Image.
        /// </summary>
        public bool IsSpecialShape => ShapeType == ShapeType.Text || ShapeType == ShapeType.Image;

        #endregion

        #region Modifiers

        /// <summary>
        /// The tags used to identify a group of objects or object properties.
        /// </summary>
        public List<string> tags = new List<string>();

        /// <summary>
        /// Modifiers the object contains.
        /// </summary>
        public List<Modifier<BeatmapObject>> modifiers = new List<Modifier<BeatmapObject>>();

        /// <summary>
        /// If modifiers ignore the lifespan restriction.
        /// </summary>
        public bool ignoreLifespan = false;

        /// <summary>
        /// If the order of triggers and actions matter.
        /// </summary>
        public bool orderModifiers = false;

        /// <summary>
        /// Variable set and used by modifiers.
        /// </summary>
        public int integerVariable;

        /// <summary>
        /// Variable set and used by modifiers.
        /// </summary>
        public float floatVariable;

        /// <summary>
        /// Variable set and used by modifiers.
        /// </summary>
        public string stringVariable = string.Empty;

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
        /// If the modifiers are currently active.
        /// </summary>
        public bool ModifiersActive
        {
            get
            {
                var startTime = ignoreLifespan ? 0f : StartTime;
                var killTime = ignoreLifespan ? SoundManager.inst.MusicLength : StartTime + SpawnDuration;
                return AudioManager.inst.CurrentAudioSource.time >= startTime && AudioManager.inst.CurrentAudioSource.time <= killTime;
            }
        }

        #endregion

        #region Prefab

        /// <summary>
        /// Used for objects spawned from a Prefab Object.
        /// </summary>
        public string originalID;

        /// <summary>
        /// If the object is spawned from a prefab and has no parent.
        /// </summary>
        public bool fromPrefabBase;

        /// <summary>
        /// If the object is spawned from a prefab.
        /// </summary>
        public bool fromPrefab;

        /// <summary>
        /// Prefab reference ID.
        /// </summary>
        public string prefabID = string.Empty;

        /// <summary>
        /// Prefab Object reference ID.
        /// </summary>
        public string prefabInstanceID = string.Empty;

        public string OriginalID { get => originalID; set => originalID = value; }

        public string PrefabID { get => prefabID; set => prefabID = value; }

        public string PrefabInstanceID { get => prefabInstanceID; set => prefabInstanceID = value; }

        public bool FromPrefab { get => fromPrefab; set => fromPrefab = value; }

        #endregion

        #region References

        /// <summary>
        /// For object modifiers.
        /// </summary>
        public List<Component> components = new List<Component>();

        /// <summary>
        /// Rigidbody for modifiers.
        /// </summary>
        public Rigidbody2D rigidbody;

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
        /// Cached runtime object.
        /// </summary>
        public RTBeatmapObject runtimeObject;

        /// <summary>
        /// Cached sequences for the runtime object.
        /// </summary>
        public CachedSequences cachedSequences;

        /// <summary>
        /// Cached runtime modifiers.
        /// </summary>
        public RTModifiers<BeatmapObject> runtimeModifiers;

        /// <summary>
        /// Used for editor.
        /// </summary>
        public TimelineObject timelineObject;

        /// <summary>
        /// Use for object modifiers.
        /// </summary>
        public Detector detector;

        #endregion

        #endregion

        #region Constants

        /// <summary>
        /// Camera parent ID.
        /// </summary>
        public const string CAMERA_PARENT = "CAMERA_PARENT";

        #endregion

        #region Methods

        public override void CopyData(BeatmapObject orig, bool newID = true)
        {
            id = newID ? LSText.randomString(16) : orig.id;
            parent = orig.parent;
            name = orig.name;
            StartTime = orig.StartTime;
            autoKillOffset = orig.autoKillOffset;
            autoKillType = orig.autoKillType;
            Depth = orig.Depth;
            editorData = ObjectEditorData.DeepCopy(orig.editorData);
            fromPrefab = orig.fromPrefab;
            objectType = orig.objectType;
            origin = orig.origin;

            SetPrefabReference(orig);

            gradientType = orig.gradientType;
            gradientScale = orig.gradientScale;
            gradientRotation = orig.gradientRotation;
            shape = orig.shape;
            shapeOption = orig.shapeOption;
            polygonShapeSettings = new PolygonShape(orig.polygonShapeSettings.Sides, orig.polygonShapeSettings.Roundness, orig.polygonShapeSettings.Thickness, orig.polygonShapeSettings.Slices);
            autoTextAlign = orig.autoTextAlign;
            text = orig.text;
            LDM = orig.LDM;
            parentType = orig.parentType;
            parentOffsets = orig.parentOffsets.Copy();
            parentAdditive = orig.parentAdditive;
            parallaxSettings = orig.parallaxSettings.Copy();
            tags = !orig.tags.IsEmpty() ? orig.tags.Clone() : new List<string>();
            renderLayerType = orig.renderLayerType;
            ignoreLifespan = orig.ignoreLifespan;
            orderModifiers = orig.orderModifiers;
            opacityCollision = orig.opacityCollision;
            desync = orig.desync;

            for (int i = 0; i < events.Count; i++)
                events[i].AddRange(orig.events[i].Select(x => x.Copy()));

            modifiers = !orig.modifiers.IsEmpty() ? orig.modifiers.Select(x => Modifier<BeatmapObject>.DeepCopy(x, this)).ToList() : new List<Modifier<BeatmapObject>>();
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            var events = new List<List<EventKeyframe>>();
            events.Add(new List<EventKeyframe>());
            events.Add(new List<EventKeyframe>());
            events.Add(new List<EventKeyframe>());
            events.Add(new List<EventKeyframe>());

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

                        eventKeyframe.time = kfjn["t"].AsFloat;

                        if (kfjn["ct"] != null)
                            eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

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

                        eventKeyframe.time = kfjn["t"].AsFloat;

                        if (kfjn["ct"] != null)
                            eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                        eventKeyframe.SetEventValues(
                            kfjn["ev"][0].AsFloat,
                            kfjn["ev"][1].AsFloat);

                        eventKeyframe.random = kfjn["r"].AsInt;

                        eventKeyframe.SetEventRandomValues(
                            kfjn["er"][0].AsFloat,
                            kfjn["er"][1].AsFloat,
                            kfjn["er"][2].AsFloat);

                        eventKeyframe.relative = false;
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

                        eventKeyframe.time = kfjn["t"].AsFloat;

                        if (kfjn["ct"] != null)
                            eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

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

                        eventKeyframe.time = kfjn["t"].AsFloat;

                        if (kfjn["ct"] != null)
                            eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

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
                        events[3].Add(eventKeyframe);
                    }
                }
            }

            this.events = events;

            id = jn["id"] ?? LSText.randomString(16);

            opacityCollision = true;

            if (jn["pre_iid"] != null)
                prefabInstanceID = jn["pre_iid"];

            if (jn["pre_id"] != null)
                prefabID = jn["pre_id"];

            if (jn["p_id"] != null)
                Parent = isCameraParented ? CAMERA_PARENT : jn["p_id"];

            if (!isCameraParented && jn["p_t"] != null)
                parentType = jn["p_t"];
            if (isCameraParented)
                parentType = "111";

            if (jn["p_o"] != null && jn["p_id"] != "camera")
                for (int i = 0; i < parentOffsets.Length; i++)
                    if (jn["p_o"].Count > i && jn["p_o"][i] != null)
                        parentOffsets[i] = jn["p_o"][i].AsFloat;
                    else if (jn["p_id"] == "camera")
                        parentOffsets = new float[3] { 0f, 0f, 0f };

            if (jn["ot"] != null)
            {
                var ot = jn["ot"].AsInt;

                objectType = ot == 4 ? ObjectType.Normal : ot == 5 ? ObjectType.Decoration : ot == 6 ? ObjectType.Empty : (ObjectType)ot;
            }

            if (jn["st"] != null)
                startTime = jn["st"].AsFloat;

            if (jn["n"] != null)
                name = jn["n"];

            if (jn["d"] != null)
                Depth = jn["d"].AsInt;
            else
                Depth = version == new Version(ProjectArrhythmia.Versions.DEPTH_DEFAULT_CHANGED) ? 0 : 20; // fixes default depth not being correct

            if (jn["s"] != null)
                shape = jn["s"].AsInt;

            if (jn["so"] != null)
                shapeOption = jn["so"].AsInt;

            if (jn["csp"] != null)
            {
                ShapeType = ShapeType.Polygon;
                polygonShapeSettings = PolygonShape.Parse(jn["csp"]);
            }

            autoTextAlign = ShapeType == ShapeType.Text;

            if (jn["gt"] != null)
                gradientType = (GradientType)jn["gt"].AsInt;

            if (jn["gs"] != null)
                gradientScale = jn["gs"].AsFloat;

            if (jn["gr"] != null)
                gradientRotation = jn["gr"].AsFloat;

            if (jn["text"] != null)
                text = jn["text"];

            if (jn["ak_t"] != null)
                autoKillType = (AutoKillType)jn["ak_t"].AsInt;

            if (jn["ak_o"] != null)
                autoKillOffset = jn["ak_o"].AsFloat;

            if (jn["o"] != null)
                origin = new Vector2(jn["o"]["x"].AsFloat, jn["o"]["y"].AsFloat);

            if (jn["ed"] != null)
                editorData = ObjectEditorData.ParseVG(jn["ed"]);
        }

        public override void ReadJSON(JSONNode jn)
        {
            var events = new List<List<EventKeyframe>>();
            events.Add(new List<EventKeyframe>());
            events.Add(new List<EventKeyframe>());
            events.Add(new List<EventKeyframe>());
            events.Add(new List<EventKeyframe>());
            if (jn["events"] != null)
            {
                // Position
                for (int i = 0; i < jn["events"]["pos"].Count; i++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"]["pos"][i];

                    eventKeyframe.id = LSText.randomNumString(8);

                    eventKeyframe.time = kfjn["t"].AsFloat;

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

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

                    events[0].Add(eventKeyframe);
                }

                // Scale
                for (int j = 0; j < jn["events"]["sca"].Count; j++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"]["sca"][j];

                    eventKeyframe.id = LSText.randomNumString(8);

                    eventKeyframe.time = kfjn["t"].AsFloat;

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.SetEventValues(kfjn["x"].AsFloat, kfjn["y"].AsFloat);

                    eventKeyframe.random = kfjn["r"].AsInt;
                    eventKeyframe.SetEventRandomValues(kfjn["rx"].AsFloat, kfjn["ry"].AsFloat, kfjn["rz"].AsFloat);

                    eventKeyframe.relative = !string.IsNullOrEmpty(kfjn["rel"]) && kfjn["rel"].AsBool;
                    eventKeyframe.locked = !string.IsNullOrEmpty(kfjn["l"]) && kfjn["l"].AsBool;

                    events[1].Add(eventKeyframe);
                }

                // Rotation
                for (int k = 0; k < jn["events"]["rot"].Count; k++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"]["rot"][k];

                    eventKeyframe.id = LSText.randomNumString(8);

                    eventKeyframe.time = kfjn["t"].AsFloat;

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

                    eventKeyframe.SetEventValues(kfjn["x"].AsFloat);

                    eventKeyframe.random = kfjn["r"].AsInt;
                    eventKeyframe.SetEventRandomValues(kfjn["rx"].AsFloat, kfjn["ry"].AsFloat, kfjn["rz"].AsFloat);

                    eventKeyframe.relative = string.IsNullOrEmpty(kfjn["rel"]) || kfjn["rel"].AsBool;
                    eventKeyframe.locked = !string.IsNullOrEmpty(kfjn["l"]) && kfjn["l"].AsBool;

                    events[2].Add(eventKeyframe);
                }

                // Color
                for (int l = 0; l < jn["events"]["col"].Count; l++)
                {
                    var eventKeyframe = new EventKeyframe();
                    var kfjn = jn["events"]["col"][l];

                    eventKeyframe.id = LSText.randomNumString(8);

                    eventKeyframe.time = kfjn["t"].AsFloat;

                    if (kfjn["ct"] != null)
                        eventKeyframe.curve = Parser.TryParse(kfjn["ct"], Easing.Linear);

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

                    events[3].Add(eventKeyframe);
                }
            }

            this.events = events;

            id = jn["id"] ?? LSText.randomString(16);

            if (jn["piid"] != null)
                prefabInstanceID = jn["piid"];

            if (jn["pid"] != null)
                prefabID = jn["pid"];

            if (jn["p"] != null)
                Parent = jn["p"];

            if (jn["pt"] != null)
                parentType = jn["pt"];

            if (jn["po"] != null)
                for (int i = 0; i < parentOffsets.Length; i++)
                    if (jn["po"].Count > i && jn["po"][i] != null)
                        parentOffsets[i] = jn["po"][i].AsFloat;

            if (jn["ps"] != null)
            {
                for (int i = 0; i < parallaxSettings.Length; i++)
                {
                    if (jn["ps"].Count > i && jn["ps"][i] != null)
                        parallaxSettings[i] = jn["ps"][i].AsFloat;
                }
            }

            if (jn["pa"] != null)
                parentAdditive = jn["pa"];

            if (jn["d"] != null)
                Depth = jn["d"].AsInt;

            if (jn["rdt"] != null)
                renderLayerType = (RenderLayerType)jn["rdt"].AsInt;

            if (jn["opcol"] != null)
                opacityCollision = jn["opcol"].AsBool;

            if (jn["iglif"] != null)
                ignoreLifespan = jn["iglif"].AsBool;

            if (jn["ordmod"] != null)
                orderModifiers = jn["ordmod"].AsBool;

            if (jn["desync"] != null && !string.IsNullOrEmpty(Parent))
                desync = jn["desync"].AsBool;

            if (jn["empty"] != null)
                objectType = jn["empty"].AsBool ? ObjectType.Empty : ObjectType.Normal;
            else if (jn["h"] != null)
                objectType = jn["h"].AsBool ? ObjectType.Helper : ObjectType.Normal;
            else if (jn["ot"] != null)
                objectType = (ObjectType)jn["ot"].AsInt;

            if (jn["ldm"] != null)
                LDM = jn["ldm"].AsBool;

            if (jn["st"] != null)
                startTime = jn["st"].AsFloat;

            if (jn["name"] != null)
                name = ((string)jn["name"]).Replace("{{colon}}", ":");

            if (jn["tags"] != null)
                for (int i = 0; i < jn["tags"].Count; i++)
                    tags.Add(((string)jn["tags"][i]).Replace("{{colon}}", ":"));

            if (jn["s"] != null)
                shape = jn["s"].AsInt;

            if (jn["shape"] != null)
                shape = jn["shape"].AsInt;

            if (jn["so"] != null)
                shapeOption = jn["so"].AsInt;

            if (jn["csp"] != null)
                polygonShapeSettings = PolygonShape.Parse(jn["csp"]);

            if (jn["gt"] != null)
                gradientType = (GradientType)jn["gt"].AsInt;

            if (jn["gs"] != null)
                gradientScale = jn["gs"].AsFloat;

            if (jn["gr"] != null)
                gradientRotation = jn["gr"].AsFloat;

            if (jn["text"] != null)
                text = ((string)jn["text"]).Replace("{{colon}}", ":");

            if (jn["ata"] != null)
                autoTextAlign = jn["ata"];

            if (jn["ak"] != null)
                autoKillType = jn["ak"].AsBool ? AutoKillType.LastKeyframe : AutoKillType.NoAutokill;
            else if (jn["akt"] != null)
                autoKillType = (AutoKillType)jn["akt"].AsInt;

            if (jn["ako"] != null)
                autoKillOffset = jn["ako"].AsFloat;

            if (jn["o"] != null)
                origin = jn["o"].AsVector2();

            if (jn["ed"] != null)
                editorData = ObjectEditorData.Parse(jn["ed"]);

            for (int i = 0; i < jn["modifiers"].Count; i++)
            {
                var modifier = Modifier<BeatmapObject>.Parse(jn["modifiers"][i], this);
                if (ModifiersHelper.VerifyModifier(modifier, ModifiersManager.defaultBeatmapObjectModifiers))
                    modifiers.Add(modifier);
            }
        }

        public override JSONNode ToJSONVG()
        {
            var jn = JSON.Parse("{}");

            jn["id"] = id;
            if (!string.IsNullOrEmpty(prefabID))
                jn["pre_id"] = prefabID;

            if (!string.IsNullOrEmpty(prefabInstanceID))
                jn["pre_iid"] = prefabInstanceID;

            if (parentType != "101")
                jn["p_t"] = parentType;

            if (parentOffsets.Any(x => x != 0f))
                for (int i = 0; i < parentOffsets.Length; i++)
                    jn["p_o"][i] = parentOffsets[i];

            if (!string.IsNullOrEmpty(parent))
                jn["p_id"] = parent == CAMERA_PARENT ? "camera" : parent;

            jn["d"] = Depth;

            jn["st"] = StartTime;

            if (!string.IsNullOrEmpty(name))
                jn["n"] = name;

            jn["ot"] = (int)objectType == 4 ? 0 : (int)objectType;
            jn["ak_t"] = (int)autoKillType;
            jn["ak_o"] = autoKillOffset;

            if (ShapeType == ShapeType.Polygon)
            {
                jn["s"] = 3; // for some reason the polygon is under the arrow shape group???
                jn["so"] = 2;

                if (polygonShapeSettings != null)
                    jn["csp"] = polygonShapeSettings.ToJSON();
            }
            else
            {
                if (shape != 0)
                    jn["s"] = shape;

                if (shapeOption != 0)
                    jn["so"] = shapeOption;
            }

            if (gradientType != GradientType.Normal)
            {
                jn["gt"] = (int)gradientType;
                if (gradientScale != 1f)
                    jn["gs"] = gradientScale;
                if (gradientRotation != 0f)
                    jn["gr"] = gradientRotation;
            }

            if (!string.IsNullOrEmpty(text))
                jn["text"] = text;

            jn["o"]["x"] = origin.x;
            jn["o"]["y"] = origin.y;

            if (editorData.locked)
                jn["ed"]["lk"] = editorData.locked;

            if (editorData.collapse)
                jn["ed"]["co"] = editorData.collapse;

            jn["ed"]["b"] = editorData.Bin;

            jn["ed"]["l"].AsInt = Mathf.Clamp(editorData.Layer, 0, 5);

            // Events
            {
                // Position
                for (int j = 0; j < events[0].Count; j++)
                {
                    var eventKeyframe = events[0][j];
                    jn["e"][0]["k"][j]["t"] = eventKeyframe.time;
                    jn["e"][0]["k"][j]["ct"] = eventKeyframe.curve.ToString();

                    jn["e"][0]["k"][j]["ev"][0] = eventKeyframe.values[0];
                    jn["e"][0]["k"][j]["ev"][1] = eventKeyframe.values[1];

                    jn["e"][0]["k"][j]["r"] = eventKeyframe.random;

                    jn["e"][0]["k"][j]["er"][0] = eventKeyframe.randomValues[0];
                    jn["e"][0]["k"][j]["er"][1] = eventKeyframe.randomValues[1];
                    jn["e"][0]["k"][j]["er"][2] = eventKeyframe.randomValues[2];
                }

                // Scale
                for (int j = 0; j < events[1].Count; j++)
                {
                    var eventKeyframe = events[1][j];
                    jn["e"][1]["k"][j]["t"] = eventKeyframe.time;
                    jn["e"][1]["k"][j]["ct"] = eventKeyframe.curve.ToString();

                    jn["e"][1]["k"][j]["ev"][0] = eventKeyframe.values[0];
                    jn["e"][1]["k"][j]["ev"][1] = eventKeyframe.values[1];

                    jn["e"][1]["k"][j]["r"] = eventKeyframe.random;

                    jn["e"][1]["k"][j]["er"][0] = eventKeyframe.randomValues[0];
                    jn["e"][1]["k"][j]["er"][1] = eventKeyframe.randomValues[1];
                    jn["e"][1]["k"][j]["er"][2] = eventKeyframe.randomValues[2];
                }

                // Rotation
                for (int j = 0; j < events[2].Count; j++)
                {
                    var eventKeyframe = events[2][j];
                    jn["e"][2]["k"][j]["t"] = eventKeyframe.time;
                    jn["e"][2]["k"][j]["ct"] = eventKeyframe.curve.ToString();

                    jn["e"][2]["k"][j]["ev"][0] = eventKeyframe.values[0];

                    if (!eventKeyframe.relative)
                        jn["e"][2]["k"][j]["ev"][1] = 1.0f;

                    jn["e"][2]["k"][j]["r"] = eventKeyframe.random;

                    jn["e"][2]["k"][j]["er"][0] = eventKeyframe.randomValues[0];
                    jn["e"][2]["k"][j]["er"][1] = eventKeyframe.randomValues[1];
                    jn["e"][2]["k"][j]["er"][2] = eventKeyframe.randomValues[2];
                }

                // Color
                for (int j = 0; j < events[3].Count; j++)
                {
                    var eventKeyframe = events[3][j];
                    jn["e"][3]["k"][j]["t"] = eventKeyframe.time;
                    jn["e"][3]["k"][j]["ct"] = eventKeyframe.curve.ToString();

                    jn["e"][3]["k"][j]["ev"][0] = Mathf.Clamp(eventKeyframe.values[0], 0, 8);
                    jn["e"][3]["k"][j]["ev"][1] = (-eventKeyframe.values[1] + 1f) * 100f;
                    if (eventKeyframe.values.Length > 5)
                        jn["e"][3]["k"][j]["ev"][2] = Mathf.Clamp(eventKeyframe.values[5], 0, 8);
                }
            }

            return jn;
        }

        public override JSONNode ToJSON()
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
                for (int i = 0; i < parentOffsets.Length; i++)
                    jn["po"][i] = parentOffsets[i];

            if (parentAdditive != "000")
                jn["pa"] = parentAdditive;

            if (parallaxSettings.Any(x => x != 1f))
                for (int i = 0; i < parallaxSettings.Length; i++)
                    jn["ps"][i] = parallaxSettings[i];

            if (!string.IsNullOrEmpty(parent))
                jn["p"] = parent;

            if (Depth != 15)
                jn["d"] = Depth;
            if (renderLayerType != RenderLayerType.Foreground)
                jn["rdt"] = (int)renderLayerType;
            if (opacityCollision)
                jn["opcol"] = opacityCollision;
            if (ignoreLifespan)
                jn["iglif"] = ignoreLifespan;
            if (orderModifiers)
                jn["ordmod"] = orderModifiers;
            if (desync && !string.IsNullOrEmpty(parent))
                jn["desync"] = desync;

            if (LDM)
                jn["ldm"] = LDM;

            jn["st"] = StartTime;

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;

            jn["ot"] = (int)objectType;
            jn["akt"] = (int)autoKillType;
            jn["ako"] = autoKillOffset;

            if (gradientType != GradientType.Normal)
            {
                jn["gt"] = (int)gradientType;
                if (gradientScale != 1f)
                    jn["gs"] = gradientScale;
                if (gradientRotation != 0f)
                    jn["gr"] = gradientRotation;
            }

            if (shape != 0)
                jn["s"] = shape;

            if (shapeOption != 0)
                jn["so"] = shapeOption;

            if (ShapeType == ShapeType.Polygon && polygonShapeSettings != null)
                jn["csp"] = polygonShapeSettings.ToJSON();

            if (!string.IsNullOrEmpty(text))
                jn["text"] = text;

            if (autoTextAlign)
                jn["ata"] = autoTextAlign;

            if (tags != null && !tags.IsEmpty())
                for (int i = 0; i < tags.Count; i++)
                    jn["tags"][i] = tags[i];

            if (origin.x != 0f || origin.y != 0f)
                jn["o"] = origin.ToJSON();

            if (editorData.ShouldSerialize)
                jn["ed"] = editorData.ToJSON();

            for (int i = 0; i < events[0].Count; i++)
                jn["events"]["pos"][i] = events[0][i].ToJSON();
            for (int i = 0; i < events[1].Count; i++)
                jn["events"]["sca"][i] = events[1][i].ToJSON();
            for (int i = 0; i < events[2].Count; i++)
                jn["events"]["rot"][i] = events[2][i].ToJSON(true);
            for (int i = 0; i < events[3].Count; i++)
                jn["events"]["col"][i] = events[3][i].ToJSON(maxValuesToSave: gradientType != GradientType.Normal ? -1 : 5);

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
        public float GetObjectLifeLength(float offset = 0f, bool oldStyle = false, bool collapse = false) => collapse && editorData.collapse ? 0.2f : autoKillType switch
        {
            AutoKillType.NoAutokill => oldStyle ? AudioManager.inst.CurrentAudioSource.clip.length - startTime : Length + offset,
            AutoKillType.LastKeyframe => Length + offset,
            AutoKillType.LastKeyframeOffset => Length + autoKillOffset + offset,
            AutoKillType.FixedTime => autoKillOffset,
            AutoKillType.SongTime => (startTime >= autoKillOffset) ? 0.1f : (autoKillOffset - startTime),
            _ => 0f,
        };

        /// <summary>
        /// Automatically applies autokill to child objects if the scale is 0.
        /// </summary>
        /// <param name="beatmapObjects">Beatmap Object to apply to.</param>
        public void SetAutokillToScale(List<BeatmapObject> beatmapObjects)
        {
            try
            {
                var parent = Parent;
                var beatmapObject = this;
                var spawnDuration = SpawnDuration;

                if (beatmapObject.events != null && !beatmapObject.events.IsEmpty() && beatmapObject.events[1].Last().time < spawnDuration &&
                    (beatmapObject.events[1].Last().values[0] == 0f || beatmapObject.events[1].Last().values[1] == 0f ||
                    beatmapObject.events[1].Last().values[0] == 0.001f || beatmapObject.events[1].Last().values[1] == 0.001f) &&
                    beatmapObject.parentType[1] == '1')
                {
                    autoKillType = AutoKillType.SongTime;
                    autoKillOffset = beatmapObject.StartTime + beatmapObject.events[1].Last().time;
                    return;
                }

                while (!string.IsNullOrEmpty(parent) && beatmapObjects.Any(x => x.id == parent))
                {
                    beatmapObject = beatmapObjects.Find(x => x.id == parent);
                    parent = beatmapObject.Parent;

                    if (beatmapObject.events != null && !beatmapObject.events.IsEmpty() && beatmapObject.events[1].Last().time < spawnDuration &&
                        (beatmapObject.events[1].Last().values[0] == 0f || beatmapObject.events[1].Last().values[1] == 0f ||
                        beatmapObject.events[1].Last().values[0] == 0.001f || beatmapObject.events[1].Last().values[1] == 0.001f) &&
                        beatmapObject.parentType[1] == '1')
                    {
                        autoKillType = AutoKillType.SongTime;
                        autoKillOffset = beatmapObject.StartTime + beatmapObject.events[1].Last().time;
                        break;
                    }
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// Gets or creates an Event Keyframe. Used for object dragging.
        /// </summary>
        /// <param name="type">Animation event type.</param>
        /// <param name="createKeyframe">If a keyframe should be created when no keyframe is found.</param>
        /// <param name="useNearest">If nearest keyframe should be searched for.</param>
        /// <param name="usePrevious">If the previous keyframe should be searched for.</param>
        /// <param name="renderEditor">If the editor should be rendered.</param>
        /// <returns>Returns the found keyframe.</returns>
        public EventKeyframe GetOrCreateKeyframe(int type, bool createKeyframe, bool useNearest = true, bool usePrevious = true, bool renderEditor = true)
        {
            var timeOffset = AudioManager.inst.CurrentAudioSource.time - StartTime;
            int nextIndex = events[type].FindIndex(x => x.time >= timeOffset);
            if (nextIndex < 0)
                nextIndex = events[type].Count - 1;

            int index;
            EventKeyframe selected;
            if (useNearest && events[type].TryFindIndex(x => x.time > timeOffset - 0.1f && x.time < timeOffset + 0.1f, out int sameIndex))
            {
                selected = events[type][sameIndex];
                index = sameIndex;
                AudioManager.inst.SetMusicTime(selected.time + StartTime);
            }
            else if (createKeyframe)
            {
                selected = events[type][nextIndex].Copy();
                selected.time = timeOffset;
                index = events[type].Count;
                events[type].Add(selected);
            }
            else if (usePrevious)
            {
                index = events[type].FindLastIndex(x => x.time < timeOffset);
                selected = events[type][index];
            }
            else
            {
                index = 0;
                selected = events[type][index];
            }

            if (renderEditor && ObjectEditor.inst)
            {
                ObjectEditor.inst.RenderKeyframes(this);
                ObjectEditor.inst.SetCurrentKeyframe(this, type, index, false, false);
            }

            return selected;
        }

        #region Prefabable

        public IRTObject GetRuntimeObject() => runtimeObject;

        public void RemovePrefabReference()
        {
            prefabID = "";
            prefabInstanceID = "";
        }

        public void SetPrefabReference(PrefabObject prefabObject)
        {
            prefabID = prefabObject.prefabID;
            prefabInstanceID = prefabObject.id;
        }

        public void SetPrefabReference(IPrefabable prefabable)
        {
            prefabID = prefabable.PrefabID;
            prefabInstanceID = prefabable.PrefabInstanceID;
        }

        public Prefab GetPrefab() => GameData.Current.prefabs.Find(x => x.id == prefabID);

        public bool TryGetPrefabObject(out PrefabObject result)
        {
            result = GetPrefabObject();
            return result;
        }

        public PrefabObject GetPrefabObject() => GameData.Current.prefabObjects.Find(x => x.id == prefabInstanceID);

        #endregion

        #region Custom Interpolation

        /// <summary>
        /// Resets the transform offsets.
        /// </summary>
        public void ResetOffsets()
        {
            reactivePositionOffset = Vector3.zero;
            reactiveScaleOffset = Vector3.zero;
            reactiveRotationOffset = 0f;
            positionOffset = Vector3.zero;
            scaleOffset = Vector3.zero;
            rotationOffset = Vector3.zero;
        }

        /// <summary>
        /// Gets a transform offset from the object.
        /// </summary>
        /// <param name="type">
        /// The type of transform value to get.<br></br>
        /// 0 -> <see cref="positionOffset"/><br></br>
        /// 1 -> <see cref="scaleOffset"/><br></br>
        /// 2 -> <see cref="rotationOffset"/>
        /// </param>
        /// <returns>Returns a transform offset.</returns>
        public Vector3 GetTransformOffset(int type) => type switch
        {
            0 => positionOffset,
            1 => scaleOffset,
            _ => rotationOffset,
        };

        /// <summary>
        /// Sets a transform offset of the object.
        /// </summary>
        /// <param name="type">
        /// The type of transform value to get.<br></br>
        /// 0 -> <see cref="positionOffset"/><br></br>
        /// 1 -> <see cref="scaleOffset"/><br></br>
        /// 2 -> <see cref="rotationOffset"/>
        /// </param>
        /// <param name="value">Value to assign to the offset.</param>
        public void SetTransform(int type, Vector3 value)
        {
            switch (type)
            {
                case 0: {
                        positionOffset = value;
                        break;
                    }
                case 1: {
                        scaleOffset = value;
                        break;
                    }
                case 2: {
                        rotationOffset = value;
                        break;
                    }
            }
        }

        /// <summary>
        /// Sets a transform offset of the object.
        /// </summary>
        /// <param name="type">
        /// The type of transform value to get.<br></br>
        /// 0 -> <see cref="positionOffset"/><br></br>
        /// 1 -> <see cref="scaleOffset"/><br></br>
        /// 2 -> <see cref="rotationOffset"/>
        /// </param>
        /// <param name="axis">The axis of the transform value to get.</param>
        /// <param name="value">Value to assign to the offset's axis.</param>
        public void SetTransform(int type, int axis, float value)
        {
            switch (type)
            {
                case 0: {
                        positionOffset[axis] = value;
                        break;
                    }
                case 1: {
                        scaleOffset[axis] = value;
                        break;
                    }
                case 2: {
                        rotationOffset[axis] = value;
                        break;
                    }
            }
        }

        /// <summary>
        /// Interpolates an animation from the object.
        /// </summary>
        /// <param name="type">
        /// The type of transform value to get.<br></br>
        /// 0 -> <see cref="positionOffset"/><br></br>
        /// 1 -> <see cref="scaleOffset"/><br></br>
        /// 2 -> <see cref="rotationOffset"/>
        /// </param>
        /// <param name="valueIndex">Axis index to interpolate.</param>
        /// <returns>Returns a single value based on the event.</returns>
        public float Interpolate(int type, int valueIndex) => Interpolate(type, valueIndex, RTLevel.Current.CurrentTime - StartTime);

        /// <summary>
        /// Interpolates an animation from the object.
        /// </summary>
        /// <param name="type">
        /// The type of transform value to get.<br></br>
        /// 0 -> <see cref="positionOffset"/><br></br>
        /// 1 -> <see cref="scaleOffset"/><br></br>
        /// 2 -> <see cref="rotationOffset"/>
        /// </param>
        /// <param name="valueIndex">Axis index to interpolate.</param>
        /// <param name="time">Time to interpolate to.</param>
        /// <returns>Returns a single value based on the event.</returns>
        public float Interpolate(int type, int valueIndex, float time)
        {
            var list = events[type].OrderBy(x => x.time).ToList();

            var nextKFIndex = list.FindIndex(x => x.time > time);

            if (nextKFIndex < 0)
                nextKFIndex = list.Count - 1;

            var prevKFIndex = nextKFIndex - 1;
            if (prevKFIndex < 0)
                prevKFIndex = 0;

            var nextKF = list[nextKFIndex];
            var prevKF = list[prevKFIndex];

            type = Mathf.Clamp(type, 0, list.Count);
            valueIndex = Mathf.Clamp(valueIndex, 0, list[0].values.Length);

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

            bool isLerper = type != 3 || valueIndex != 0;

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

        /// <summary>
        /// Interpolates an animation from the objects' parent chain.
        /// </summary>
        /// <param name="type">
        /// The type of transform value to get.<br></br>
        /// 0 -> <see cref="positionOffset"/><br></br>
        /// 1 -> <see cref="scaleOffset"/><br></br>
        /// 2 -> <see cref="rotationOffset"/>
        /// </param>
        /// <param name="valueIndex">Axis index to interpolate.</param>
        /// <param name="time">Time to interpolate to.</param>
        /// <returns>Returns a single value based on the event.</returns>
        public float InterpolateChain(int type, int valueIndex, float time, MathOperation operation = MathOperation.Addition)
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
        public Vector3 InterpolateChainPosition(bool includeDepth = false, bool includeOffsets = true, bool includeSelf = true) => InterpolateChainPosition(RTLevel.Current.CurrentTime - StartTime, includeDepth, includeOffsets, includeSelf);

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

        public Vector2 InterpolateChainScale(bool includeOffsets = true, bool includeSelf = true) => InterpolateChainScale(RTLevel.Current.CurrentTime - StartTime, includeOffsets, includeSelf);

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

        public float InterpolateChainRotation(bool includeOffsets = true, bool includeSelf = true) => InterpolateChainRotation(RTLevel.Current.CurrentTime - StartTime, includeOffsets, includeSelf);

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

        public ObjectTransform InterpolateChain(bool includeDepth = false, bool includeOffsets = true, bool includeSelf = true) => InterpolateChain(RTLevel.Current.CurrentTime - StartTime, includeDepth, includeOffsets, includeSelf);

        public ObjectTransform InterpolateChain(float time, bool includeDepth = false, bool includeOffsets = true, bool includeSelf = true)
        {
            var result = ObjectTransform.Default;

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

            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
            {
                var transform = runtimeObject.visualObject.gameObject.transform;

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

            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
            {
                var transform = runtimeObject.visualObject.gameObject.transform;

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
        public BeatmapObject GetParent(List<BeatmapObject> beatmapObjects) => beatmapObjects.Find(x => x.id == Parent);

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

            string parent = this.Parent;
            int index = beatmapObjects.FindIndex(x => x.id == parent);

            list.Add(this);
            while (index >= 0)
            {
                list.Add(beatmapObjects[index]);
                parent = beatmapObjects[index].Parent;
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
            if (beatmapObjects.TryFindAll(x => x.Parent == id, out List<BeatmapObject> children))
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
        public List<BeatmapObject> GetChildren() => GameData.Current.beatmapObjects.TryFindAll(x => x.Parent == id, out List<BeatmapObject> children) ? children : new List<BeatmapObject>();

        public void SetParent(BeatmapObject beatmapObjectToParentTo, bool renderParent = true) => TrySetParent(beatmapObjectToParentTo, renderParent);

        /// <summary>
        /// Tries to set an objects' parent. If the parent the user is trying to assign an object to a child of the object, then don't set parent.
        /// </summary>
        /// <param name="beatmapObjectToParentTo">Object to try parenting to.</param>
        /// <param name="renderParent">If parent editor should render.</param>
        /// <returns>Returns true if the <see cref="BeatmapObject"/> was successfully parented, otherwise returns false.</returns>
        public bool TrySetParent(BeatmapObject beatmapObjectToParentTo, bool renderParent = true)
        {
            var dictionary = new Dictionary<string, bool>();
            var beatmapObjects = GameData.Current.beatmapObjects;

            foreach (var obj in beatmapObjects)
                dictionary[obj.id] = CanParent(obj, beatmapObjects);

            dictionary[id] = false;

            var shouldParent = dictionary.TryGetValue(beatmapObjectToParentTo.id, out bool value) && value;

            if (shouldParent)
            {
                Parent = beatmapObjectToParentTo.id;
                RTLevel.Current?.UpdateObject(this, RTLevel.ObjectContext.PARENT_CHAIN);

                if (renderParent)
                    ObjectEditor.inst.RenderParent(this);
            }

            return shouldParent;
        }

        /// <summary>
        /// Checks if another object can be parented to this object.
        /// </summary>
        /// <param name="obj">Object to check the parent compatibility of.</param>
        /// <returns>Returns true if <paramref name="obj"/> can be parented to this.</returns>
        public bool CanParent(BeatmapObject obj) => CanParent(obj, GameData.Current.beatmapObjects);

        /// <summary>
        /// Checks if another object can be parented to this object.
        /// </summary>
        /// <param name="obj">Object to check the parent compatibility of.</param>
        /// <param name="beatmapObjects">Beatmap objects to search through.</param>
        /// <returns>Returns true if <paramref name="obj"/> can be parented to this.</returns>
        public bool CanParent(BeatmapObject obj, List<BeatmapObject> beatmapObjects)
        {
            if (string.IsNullOrEmpty(obj.Parent))
                return true;

            bool canParent = true;
            string parentID = id;

            while (!string.IsNullOrEmpty(parentID))
            {
                if (parentID == obj.Parent)
                {
                    canParent = false;
                    break;
                }

                parentID = beatmapObjects.TryFind(x => x.Parent == parentID, out BeatmapObject parentObj) ? parentObj.id : null;
            }

            return canParent;
        }

        /// <summary>
        /// Gets the parent toggle value depending on the index.
        /// </summary>
        /// <param name="index">Index of the parent toggle to get.</param>
        /// <returns>Returns a parent toggle value.</returns>
        public bool GetParentType(int index) => parentType[index] == '1';

        /// <summary>
        /// Sets the parent toggle value depending on the index.
        /// </summary>
        /// <param name="index">Index to assign to.</param>
        /// <param name="val">The new value to set.</param>
        public void SetParentType(int index, bool val)
        {
            var stringBuilder = new StringBuilder(parentType);
            stringBuilder[index] = (val ? '1' : '0');
            parentType = stringBuilder.ToString();
            CoreHelper.Log($"Set Parent Type: {parentType}");
        }

        /// <summary>
        /// Gets the parent delay value depending on the index.
        /// </summary>
        /// <param name="index">Index of the parent delay to get.</param>
        /// <returns>Returns a parent delay value.</returns>
        public float GetParentOffset(int index) => parentOffsets.InRange(index) ? parentOffsets[index] : 0f;

        /// <summary>
        /// Sets the parent delay value depending on the index.
        /// </summary>
        /// <param name="index">Index to assign to.</param>
        /// <param name="val">the new value to set.</param>
        public void SetParentOffset(int index, float val)
        {
            if (parentOffsets.InRange(index))
                parentOffsets[index] = val;
        }

        /// <summary>
        /// Gets the parent additive value depending on the index.
        /// </summary>
        /// <param name="index">Index of the parent additive to get.</param>
        /// <returns>Returns a parent additive value.</returns>
        public bool GetParentAdditive(int index) => parentAdditive[index] == '1';

        /// <summary>
        /// Sets the parent additive value depending on the index.
        /// </summary>
        /// <param name="index">Index to assign to.</param>
        /// <param name="val">The new value to set.</param>
        public void SetParentAdditive(int index, bool val)
        {
            var stringBuilder = new StringBuilder(parentAdditive);
            stringBuilder[index] = val ? '1' : '0';
            parentAdditive = stringBuilder.ToString();
            CoreHelper.Log($"Set Parent Additive: {parentAdditive}");
        }

        #endregion

        #endregion

        #region Operators

        public override bool Equals(object obj) => obj is PrefabObject paObj && id == paObj.id;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => $"{id} - {name}";

        #endregion
    }
}
