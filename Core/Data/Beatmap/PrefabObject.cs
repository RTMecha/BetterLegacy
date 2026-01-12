using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using ILMath;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// An instance of a <see cref="Prefab"/> that spawns all objects contained in the Prefab.
    /// </summary>
    public class PrefabObject : PAObject<PrefabObject>, ILifetime, ITransformable, IParentable, IModifyable, IModifierReference, IEditable, IPrefabable, IEvaluatable
    {
        public PrefabObject() : base()
        {
            editorData.Bin = 0;
            editorData.Layer = 0;

            SetDefaultTransformOffsets();
        }

        public PrefabObject(string prefabID) : this() => this.prefabID = prefabID;

        public PrefabObject(string prefabID, float startTime) : this(prefabID) => this.startTime = startTime;

        #region Values

        /// <summary>
        /// <see cref="Prefab"/> reference ID.
        /// </summary>
        public string prefabID = string.Empty;

        /// <summary>
        /// If the Prefab Object was expanded.
        /// </summary>
        public bool expanded;

        public DetailMode detailMode;

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
        /// Prefab Object reference ID.
        /// </summary>
        public string prefabInstanceID = string.Empty;

        public string OriginalID { get => originalID; set => originalID = value; }

        public string PrefabID { get => prefabID; set => prefabID = value; }

        public string PrefabInstanceID { get => prefabInstanceID; set => prefabInstanceID = value; }

        public bool FromPrefab { get => fromPrefab; set => fromPrefab = value; }

        public Prefab CachedPrefab { get; set; }
        public PrefabObject CachedPrefabObject { get; set; }

        #endregion

        #region Parent

        public string ID { get => id; set => id = value; }

        /// <summary>
        /// ID of the object to parent this to. This value is not saved and is temporarily used for swapping parents.
        /// </summary>
        public string customParent;
        public string CustomParent { get => customParent; set => customParent = value; }

        /// <summary>
        /// ID of the object to parent all spawned base objects to.
        /// </summary>
        public string parent;
        public string Parent
        {
            get => customParent ?? parent;
            set
            {
                CachedParent = null;
                customParent = null;
                parent = value;
            }
        }

        /// <summary>
        /// Parent delay values.
        /// </summary>
        public float[] parentOffsets = new float[3]
        {
            0f,
            0f,
            0f
        };

        public float[] ParentOffsets { get => parentOffsets; set => parentOffsets = value; }

        /// <summary>
        /// Parent toggle values.
        /// </summary>
        public string parentType = BeatmapObject.DEFAULT_PARENT_TYPE;

        public string ParentType { get => parentType; set => parentType = value; }

        /// <summary>
        /// Multiplies from the parents' position, allowing for parallaxing.
        /// </summary>
        public float[] parentParallax = new float[3]
        {
            1f,
            1f,
            1f
        };

        public float[] ParentParallax { get => parentParallax; set => parentParallax = value; }

        /// <summary>
        /// If parent chains should be accounted for when parent offset / delay is used.
        /// </summary>
        public string parentAdditive = BeatmapObject.DEFAULT_PARENT_ADDITIVE;

        public string ParentAdditive { get => parentAdditive; set => parentAdditive = value; }

        /// <summary>
        /// If the object should stop following the parent chain after spawn.
        /// </summary>
        public bool desync;

        public bool ParentDesync { get => desync; set => desync = value; }

        public float ParentDesyncOffset { get; set; }

        /// <summary>
        /// Temporary detatches the parent (similar to <see cref="desync"/>).
        /// </summary>
        public bool detatched;

        public bool ParentDetatched { get => detatched; set => detatched = value; }

        /// <summary>
        /// If the Prefab Object is treated as a regular parented object.
        /// </summary>
        public bool parentSelf;

        /// <summary>
        /// If the parent desync time should include the prefab time.
        /// </summary>
        public bool offsetParentDesyncTime = true;

        public BeatmapObject CachedParent { get; set; }

        #endregion

        #region Timing

        /// <summary>
        /// The max speed of a prefab object.
        /// </summary>
        public const float MAX_PREFAB_OBJECT_SPEED = 1000f;

        float startTime;
        public float StartTime { get => startTime; set => startTime = value; }

        int repeatCount;
        /// <summary>
        /// Amount of times to spawn the <see cref="Prefab"/>'s objects.
        /// </summary>
        public int RepeatCount
        {
            get => repeatCount;
            set => repeatCount = Mathf.Clamp(value, 0, 1000);
        }

        float repeatOffsetTime;
        /// <summary>
        /// Time to offset for each spawned object.
        /// </summary>
        public float RepeatOffsetTime
        {
            get => repeatOffsetTime;
            set => repeatOffsetTime = Mathf.Clamp(value, 0f, 60f);
        }

        float speed = 1f;
        /// <summary>
        /// Speed to multiply the spawned objects to.
        /// </summary>
        public float Speed
        {
            get => Mathf.Clamp(speed, 0.01f, MAX_PREFAB_OBJECT_SPEED);
            set => speed = Mathf.Clamp(value, 0.01f, MAX_PREFAB_OBJECT_SPEED);
        }

        public PrefabAutoKillType autoKillType = PrefabAutoKillType.Regular;

        public float autoKillOffset = -1f;

        public bool Alive
        {
            get
            {
                var prefab = this.GetPrefab();
                var st = StartTime + prefab.offset;
                var time = this.GetParentRuntime().CurrentTime;
                return autoKillType switch
                {
                    PrefabAutoKillType.Regular => time >= st && time <= st + GetObjectLifeLength(prefab, noAutokill: true),
                    PrefabAutoKillType.SongTime => time >= st && time <= autoKillOffset,
                    PrefabAutoKillType.StartTimeOffset => time >= st && time <= st + autoKillOffset,
                    _ => false,
                };
            }
        }

        public float SpawnDuration => GetObjectLifeLength(noAutokill: true);

        #endregion

        #region Transforms

        /// <summary>
        /// Transform offsets.
        /// </summary>
        public List<EventKeyframe> events = new List<EventKeyframe>();

        /// <summary>
        /// Depth offset of the Prefab Object.
        /// </summary>
        public float depth;

        /// <summary>
        /// The objects' full transform values.
        /// </summary>
        public FullTransform.Struct fullTransform = FullTransform.Struct.Default;

        /// <summary>
        /// The objects' full transform offset values.
        /// </summary>
        public FullTransform.Struct fullTransformOffset = default;

        #endregion

        #region Modifiers

        public ModifierReferenceType ReferenceType => ModifierReferenceType.PrefabObject;

        /// <summary>
        /// The tags used to identify a group of objects or object properties.
        /// </summary>
        public List<string> tags = new List<string>();

        public List<string> Tags { get => tags; set => tags = value; }

        /// <summary>
        /// Modifiers the object contains.
        /// </summary>
        public List<Modifier> modifiers = new List<Modifier>();

        public List<Modifier> Modifiers { get => modifiers; set => modifiers = value; }

        /// <summary>
        /// If modifiers ignore the lifespan restriction.
        /// </summary>
        public bool ignoreLifespan = false;

        public bool IgnoreLifespan { get => ignoreLifespan; set => ignoreLifespan = value; }

        /// <summary>
        /// If the order of triggers and actions matter.
        /// </summary>
        public bool orderModifiers = false;

        public bool OrderModifiers { get => orderModifiers; set => orderModifiers = value; }

        /// <summary>
        /// Variable set and used by modifiers.
        /// </summary>
        public int integerVariable;

        public int IntVariable { get => integerVariable; set => integerVariable = value; }

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

        public Vector3 PositionOffset { get => positionOffset; set => positionOffset = value; }
        public Vector3 ScaleOffset { get => scaleOffset; set => scaleOffset = value; }
        public Vector3 RotationOffset { get => rotationOffset; set => rotationOffset = value; }

        public bool ModifiersActive
        {
            get
            {
                var prefab = this.GetPrefab();
                var startTime = ignoreLifespan ? 0f : StartTime + prefab.offset;
                var killTime = ignoreLifespan ? SoundManager.inst.MusicLength : StartTime + prefab.offset + SpawnDuration;
                var time = this.GetParentRuntime().CurrentTime;
                return time >= startTime && time <= killTime;
            }
        }

        #endregion

        #region Runtime

        public RTLevelBase ParentRuntime { get; set; }

        /// <summary>
        /// Cached runtime object.
        /// </summary>
        public RTPrefabObject runtimeObject;

        /// <summary>
        /// Cached runtime modifiers.
        /// </summary>
        public RTPrefabModifiers runtimeModifiers;

        /// <summary>
        /// Cached transform.
        /// </summary>
        public ObjectTransform cachedTransform;

        /// <summary>
        /// If the Prefab Object was spawned from a modifier.
        /// </summary>
        public bool fromModifier;

        /// <summary>
        /// Spawned objects from the <see cref="Prefab"/>.
        /// </summary>
        public List<IPrefabable> expandedObjects = new List<IPrefabable>();

        /// <summary>
        /// Expanded Beatmap Objects.
        /// </summary>
        public List<BeatmapObject> ExpandedObjects => GameData.Current.beatmapObjects.FindAll(x => x.fromPrefab && x.prefabInstanceID == id);

        #endregion

        #region Editor

        /// <summary>
        /// Data for the object in the editor.
        /// </summary>
        public ObjectEditorData editorData = new ObjectEditorData();

        public ObjectEditorData EditorData { get => editorData; set => editorData = value; }

        /// <summary>
        /// Timeline Object reference for the editor.
        /// </summary>
        public TimelineObject timelineObject;

        public TimelineObject TimelineObject { get => timelineObject; set => timelineObject = value; }

        public bool CanRenderInTimeline => !string.IsNullOrEmpty(id) && !fromModifier && !FromPrefab;

        #endregion

        #endregion

        #region Functions

        public override void CopyData(PrefabObject orig, bool newID = true)
        {
            id = newID ? LSText.randomString(16) : orig.id;
            prefabID = orig.prefabID;
            startTime = orig.StartTime;
            prefabInstanceID = orig.prefabInstanceID;

            autoKillOffset = orig.autoKillOffset;
            autoKillType = orig.autoKillType;

            detailMode = orig.detailMode;
            repeatCount = orig.repeatCount;
            repeatOffsetTime = orig.repeatOffsetTime;
            speed = orig.Speed;
            depth = orig.depth;

            CopyTransformData(orig);

            parentSelf = orig.parentSelf;
            offsetParentDesyncTime = orig.offsetParentDesyncTime;
            this.CopyParentData(orig);
            this.CopyModifyableData(orig);

            editorData.CopyData(orig.editorData);
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            if (jn["id"] != null)
                id = jn["id"];
            if (jn["pid"] != null)
                prefabID = jn["pid"];
            StartTime = jn["t"] == null ? jn["st"].AsFloat : jn["t"].AsFloat;
            if (jn["parid"] != null)
                Parent = jn["parid"];
            parentSelf = true;
            desync = true;
            offsetParentDesyncTime = false;

            if (jn["ed"] != null)
                editorData.ReadJSONVG(jn["ed"], version);

            events.Clear();

            if (jn["e"] != null)
            {
                try
                {
                    events.Add(new EventKeyframe
                    {
                        values = new float[2]
                        {
                            jn["e"][0]["ev"][0].AsFloat,
                            jn["e"][0]["ev"][1].AsFloat,
                        }
                    });
                }
                catch (System.Exception)
                {
                    events.Add(new EventKeyframe
                    {
                        values = new float[2],
                    });
                }

                try
                {
                    events.Add(new EventKeyframe
                    {
                        values = new float[2]
                        {
                            jn["e"][1]["ev"][0].AsFloat,
                            jn["e"][1]["ev"][1].AsFloat,
                        }
                    });
                }
                catch (System.Exception)
                {
                    events.Add(new EventKeyframe
                    {
                        values = new float[2] { 1f, 1f },
                    });
                }

                try
                {
                    events.Add(new EventKeyframe
                    {
                        values = new float[1] { jn["e"][2]["ev"][0].AsFloat }
                    });
                }
                catch (System.Exception)
                {
                    events.Add(new EventKeyframe
                    {
                        values = new float[1]
                    });
                }
            }
            else
                SetDefaultTransformOffsets();
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn["id"] != null)
                id = jn["id"];
            if (jn["st"] != null)
                StartTime = jn["st"].AsFloat;

            this.ReadPrefabJSON(jn);
            this.ReadParentJSON(jn);
            if (jn["pself"] != null)
                parentSelf = jn["pself"].AsBool;
            if (jn["offset_pd"] != null)
                offsetParentDesyncTime = jn["offset_pd"].AsBool;

            if (jn["rc"] != null)
                RepeatCount = jn["rc"].AsInt;

            if (jn["ro"] != null)
                RepeatOffsetTime = jn["ro"].AsFloat;

            if (jn["sp"] != null)
                Speed = jn["sp"].AsFloat;

            if (jn["akt"] != null)
                autoKillType = (PrefabAutoKillType)jn["akt"].AsInt;

            if (jn["ako"] != null)
                autoKillOffset = jn["ako"].AsFloat;

            if (jn["ed"] != null)
                editorData.ReadJSON(jn["ed"]);

            if (jn["exp"] != null)
                expanded = jn["exp"].AsBool;

            if (jn["dm"] != null)
                detailMode = (DetailMode)jn["dm"].AsInt;

            if (jn["d"] != null)
                depth = jn["d"].AsFloat;

            events.Clear();
            if (jn["e"] != null)
            {
                if (jn["e"]["pos"] != null)
                {
                    var kf = new EventKeyframe();
                    var jnpos = jn["e"]["pos"];

                    kf.SetValues(jnpos["x"].AsFloat, jnpos["y"].AsFloat);
                    kf.random = jnpos["r"].AsInt;
                    kf.SetRandomValues(jnpos["rx"].AsFloat, jnpos["ry"].AsFloat, jnpos["rz"].AsFloat);
                    events.Add(kf);
                }
                else
                    events.Add(new EventKeyframe(new float[2] { 0f, 0f }, new float[3] { 0f, 0f, 0f }));

                if (jn["e"]["sca"] != null)
                {
                    var kf = new EventKeyframe();
                    var jnsca = jn["e"]["sca"];
                    kf.SetValues(jnsca["x"].AsFloat, jnsca["y"].AsFloat);
                    kf.random = jnsca["r"].AsInt;
                    kf.SetRandomValues(jnsca["rx"].AsFloat, jnsca["ry"].AsFloat, jnsca["rz"].AsFloat);
                    events.Add(kf);
                }
                else
                    events.Add(new EventKeyframe(new float[2] { 1f, 1f }, new float[3] { 0f, 0f, 0f }));

                if (jn["e"]["rot"] != null)
                {
                    var kf = new EventKeyframe();
                    var jnrot = jn["e"]["rot"];
                    kf.SetValues(jnrot["x"].AsFloat);
                    kf.random = jnrot["r"].AsInt;
                    kf.SetRandomValues(jnrot["rx"].AsFloat, 0f, jnrot["rz"].AsFloat);
                    events.Add(kf);
                }
                else
                    events.Add(new EventKeyframe(new float[1] { 0f }, new float[3] { 0f, 0f, 0f }));
            }
            else
                SetDefaultTransformOffsets();

            if (jn["tf"] != null)
            {
                fullTransform.position = Parser.TryParse(jn["tf"]["pos"], Vector3.zero);
                fullTransform.scale = Parser.TryParse(jn["tf"]["sca"], Vector3.one);
                fullTransform.rotation = Parser.TryParse(jn["tf"]["rot"], Vector3.zero);
            }

            if (jn["tf_offset"] != null)
            {
                fullTransformOffset.position = Parser.TryParse(jn["tf_offset"]["pos"], Vector3.zero);
                fullTransformOffset.scale = Parser.TryParse(jn["tf_offset"]["sca"], Vector3.zero);
                fullTransformOffset.rotation = Parser.TryParse(jn["tf_offset"]["rot"], Vector3.zero);

                PositionOffset = fullTransformOffset.position;
                ScaleOffset = fullTransformOffset.scale;
                RotationOffset = fullTransformOffset.rotation;
            }

            this.ReadModifiersJSON(jn);
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id;
            jn["pid"] = prefabID;

            jn["ed"] = editorData.ToJSONVG();

            jn["e"][0]["ct"] = "Linear";
            jn["e"][0]["ev"][0] = events[0].values[0];
            jn["e"][0]["ev"][1] = events[0].values[1];

            jn["e"][1]["ct"] = "Linear";
            jn["e"][1]["ev"][0] = events[1].values[0];
            jn["e"][1]["ev"][1] = events[1].values[1];

            jn["e"][2]["ct"] = "Linear";
            jn["e"][2]["ev"][0] = events[2].values[0];

            jn["t"] = StartTime;

            return jn;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id;
            jn["st"] = StartTime;

            this.WritePrefabJSON(jn);
            this.WriteParentJSON(jn);
            if (parentSelf)
                jn["pself"] = parentSelf;
            if (!offsetParentDesyncTime)
                jn["offset_pd"] = offsetParentDesyncTime;

            if (Speed != 1f)
                jn["sp"] = Speed;

            if (autoKillType != PrefabAutoKillType.Regular)
            {
                jn["akt"] = (int)autoKillType;

                if (autoKillOffset != -1f)
                    jn["ako"] = autoKillOffset;
            }

            if (RepeatCount > 0)
                jn["rc"] = RepeatCount;
            if (RepeatOffsetTime > 0f)
                jn["ro"] = RepeatOffsetTime;

            if (editorData.ShouldSerialize)
                jn["ed"] = editorData.ToJSON();

            if (expanded)
                jn["exp"] = expanded;

            if (detailMode != DetailMode.Normal)
                jn["dm"] = (int)detailMode;

            if (depth != 0f)
                jn["d"] = depth;

            jn["e"]["pos"]["x"] = events[0].values[0];
            jn["e"]["pos"]["y"] = events[0].values[1];
            if (events[0].random != 0)
            {
                jn["e"]["pos"]["r"] = events[0].random;
                jn["e"]["pos"]["rx"] = events[0].randomValues[0];
                jn["e"]["pos"]["ry"] = events[0].randomValues[1];
                jn["e"]["pos"]["rz"] = events[0].randomValues[2];
            }

            jn["e"]["sca"]["x"] = events[1].values[0];
            jn["e"]["sca"]["y"] = events[1].values[1];
            if (events[1].random != 0)
            {
                jn["e"]["sca"]["r"] = events[1].random;
                jn["e"]["sca"]["rx"] = events[1].randomValues[0];
                jn["e"]["sca"]["ry"] = events[1].randomValues[1];
                jn["e"]["sca"]["rz"] = events[1].randomValues[2];
            }

            jn["e"]["rot"]["x"] = events[2].values[0];
            if (events[2].random != 0)
            {
                jn["e"]["rot"]["r"] = events[2].random;
                jn["e"]["rot"]["rx"] = events[2].randomValues[0];
                jn["e"]["rot"]["rz"] = events[2].randomValues[2];
            }

            if (fullTransform.position != Vector3.zero)
                jn["tf"]["pos"] = fullTransform.position.ToJSON();
            if (fullTransform.scale != Vector3.one)
                jn["tf"]["sca"] = fullTransform.scale.ToJSON();
            if (fullTransform.rotation != Vector3.zero)
                jn["tf"]["rot"] = fullTransform.rotation.ToJSON();

            if (fullTransformOffset.position != Vector3.zero)
                jn["tf_offset"]["pos"] = fullTransformOffset.position.ToJSON();
            if (fullTransformOffset.scale != Vector3.zero)
                jn["tf_offset"]["sca"] = fullTransformOffset.scale.ToJSON();
            if (fullTransformOffset.rotation != Vector3.zero)
                jn["tf_offset"]["rot"] = fullTransformOffset.rotation.ToJSON();

            this.WriteModifiersJSON(jn);

            return jn;
        }

        public void PasteInstanceData(PrefabObject copiedInstanceData)
        {
            autoKillOffset = copiedInstanceData.autoKillOffset;
            autoKillType = copiedInstanceData.autoKillType;

            for (int i = 0; i < events.Count; i++)
            {
                if (!copiedInstanceData.events.InRange(i))
                    return;

                var copy = copiedInstanceData.events[i];
                for (int j = 0; j < events[i].values.Length; j++)
                {
                    if (copy.values.TryGetAt(j, out float val))
                        events[i].values[j] = val;
                }
                for (int j = 0; j < events[i].randomValues.Length; j++)
                {
                    if (copy.randomValues.TryGetAt(j, out float val))
                        events[i].randomValues[j] = val;
                }
                events[i].random = copy.random;
            }

            this.CopyModifyableData(copiedInstanceData);
            this.CopyParentData(copiedInstanceData);
            parentSelf = copiedInstanceData.parentSelf;
            offsetParentDesyncTime = copiedInstanceData.offsetParentDesyncTime;
            detailMode = copiedInstanceData.detailMode;
            depth = copiedInstanceData.depth;
            RepeatCount = copiedInstanceData.RepeatCount;
            RepeatOffsetTime = copiedInstanceData.RepeatOffsetTime;
        }

        public float GetObjectLifeLength(float offset = 0.0f, bool noAutokill = false, bool collapse = false) => collapse && editorData.collapse ? 0.2f : GetObjectLifeLength(this.GetPrefab(), offset, noAutokill, collapse);

        public float GetObjectLifeLength(Prefab prefab, float offset = 0.0f, bool noAutokill = false, bool collapse = false)
        {
            float length = EditorConfig.Instance.TimelineObjectCollapseLength.Value;

            if (collapse && editorData.collapse)
                return length;
            if (!prefab)
                return length;

            var prefabables = prefab.GetPrefabables();
            if (prefabables.IsEmpty())
                return length;

            var time = prefabables.Min(x => x.StartTime);
            length = prefabables.Max(x => x.StartTime + x.GetObjectLifeLength(offset, noAutokill, collapse) - time);

            var duration = length;
            if (RepeatCount == 0)
                return ((duration + prefab.offset) / Speed) - prefab.offset;
            var t = RepeatOffsetTime != 0f ? RepeatOffsetTime : 1f;
            var timeToAdd = 0f;
            for (int i = 0; i < RepeatCount + 1; i++)
            {
                duration = (length * (i + 1)) + timeToAdd;
                timeToAdd += t;
            }

            return ((duration + prefab.offset) / Speed) - prefab.offset;
        }

        public void UpdateDefaultTransform()
        {
            fullTransformOffset.position = PositionOffset;
            fullTransformOffset.scale = ScaleOffset;
            fullTransformOffset.rotation = RotationOffset;
        }

        public void ResetOffsets()
        {
            reactivePositionOffset = Vector3.zero;
            reactiveScaleOffset = Vector3.zero;
            reactiveRotationOffset = 0f;
            PositionOffset = fullTransformOffset.position;
            ScaleOffset = fullTransformOffset.scale;
            RotationOffset = fullTransformOffset.rotation;
        }

        public Vector3 GetTransformOffset(int type) => type switch
        {
            0 => positionOffset,
            1 => scaleOffset,
            _ => rotationOffset,
        };

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

        public Vector3 GetFullPosition() => runtimeObject && runtimeObject.SpawnParent ? runtimeObject.SpawnParent.position : positionOffset + new Vector3(events[0].values[0], events[0].values[1]);

        public Vector3 GetFullScale()
        {
            var scale = scaleOffset;
            scale.x *= events[1].values[0];
            scale.y *= events[1].values[1];
            return scale.x != 0f && scale.y != 0f ? scale : Vector3.one;
        }

        public Vector3 GetFullRotation(bool includeSelf) => rotationOffset + new Vector3(0f, 0f, events[2].values[0]);

        /// <summary>
        /// Gets the transform offsets from the Prefab Object.
        /// </summary>
        /// <param name="prefabObject">Prefab Object to get the transform offsets from.</param>
        /// <returns>Returns a struct representing the objects' transform values.</returns>
        public ObjectTransform.Struct GetTransformOffset()
        {
            if (cachedTransform != null)
                return cachedTransform.ToStruct();

            var transform = ObjectTransform.Struct.Default;

            bool hasPosX = events.Count > 0 && events[0] && events[0].values.Length > 0;
            bool hasPosY = events.Count > 0 && events[0] && events[0].values.Length > 1;

            bool hasScaX = events.Count > 1 && events[1] && events[1].values.Length > 0;
            bool hasScaY = events.Count > 1 && events[1] && events[1].values.Length > 1;

            bool hasRot = events.Count > 2 && events[2] && events[2].values.Length > 0;

            transform.position = new Vector3(hasPosX ? events[0].values[0] : 0f, hasPosY ? events[0].values[1] : 0f, depth);
            transform.scale = new Vector2(hasScaX ? events[1].values[0] : 1f, hasScaY ? events[1].values[1] : 1f);
            transform.rotation = hasRot ? events[2].values[0] : 0f;

            try
            {
                if (events[0].random != 0)
                {
                    transform.position = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(id, events[0]);
                    transform.position.z = depth;
                }
                if (events[1].random != 0)
                    transform.scale = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(id, events[1]);
                if (events[2].random != 0)
                    transform.rotation = RandomHelper.KeyframeRandomizer.RandomizeFloatKeyframe(id, events[2]);
            }
            catch (System.Exception ex)
            {
                CoreHelper.LogError($"Prefab Randomization error.\nException: {ex}");
            }

            transform.scale = transform.scale.x != 0f && transform.scale.y != 0f ? transform.scale : Vector3.one;
            cachedTransform = transform.ToClass();

            return transform;
        }

        /// <summary>
        /// Copies transform data from another Prefab Object.
        /// </summary>
        /// <param name="orig">Original Prefab Object to copy transform data from.</param>
        public void CopyTransformData(PrefabObject orig)
        {
            if (events == null)
                events = new List<EventKeyframe>();
            events.Clear();

            if (orig.events != null)
                foreach (var eventKeyframe in orig.events)
                    events.Add(eventKeyframe.Copy());
        }

        public IRTObject GetRuntimeObject() => runtimeObject;

        public IPrefabable AsPrefabable() => this;
        public ITransformable AsTransformable() => this;

        public ModifierLoop GetModifierLoop() => runtimeModifiers?.loop;

        public void InterpolateAnimation(PAAnimation animation, float t) => this.InterpolateAnimationOffset(animation, t);

        public void SetDefaultTransformOffsets()
        {
            events = new List<EventKeyframe>()
            {
                new EventKeyframe(new float[2] { 0f, 0f }, new float[3] { 0f, 0f, 0f }),
                new EventKeyframe(new float[2] { 1f, 1f }, new float[3] { 0f, 0f, 0f }),
                new EventKeyframe(new float[1] { 0f }, new float[3] { 0f, 0f, 0f }),
            };
        }

        #region Evaluation

        public void SetOtherObjectVariables(Dictionary<string, float> variables)
        {
            variables["otherRuntimeFixedTime"] = this.GetParentRuntime().FixedTime;
            variables["otherRuntimeTime"] = this.GetParentRuntime().CurrentTime;

            variables["otherIntVariable"] = integerVariable;

            variables["otherObjectStartTime"] = StartTime;
            variables["otherObjectKillTime"] = SpawnDuration;

            variables["otherPositionOffsetX"] = positionOffset.x;
            variables["otherPositionOffsetY"] = positionOffset.y;
            variables["otherPositionOffsetZ"] = positionOffset.z;

            variables["otherScaleOffsetX"] = scaleOffset.x;
            variables["otherScaleOffsetY"] = scaleOffset.y;
            variables["otherScaleOffsetZ"] = scaleOffset.z;

            variables["otherRotationOffsetX"] = rotationOffset.x;
            variables["otherRotationOffsetY"] = rotationOffset.y;
            variables["otherRotationOffsetZ"] = rotationOffset.z;

            if (!runtimeObject)
                return;

            var transform = runtimeObject.Parent;
            if (!transform)
                return;

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

        public void SetObjectVariables(Dictionary<string, float> variables)
        {
            variables["runtimeFixedTime"] = this.GetParentRuntime().FixedTime;
            variables["runtimeTime"] = this.GetParentRuntime().CurrentTime;

            variables["intVariable"] = integerVariable;

            variables["objectStartTime"] = StartTime;
            variables["objectKillTime"] = SpawnDuration;

            variables["positionOffsetX"] = positionOffset.x;
            variables["positionOffsetY"] = positionOffset.y;
            variables["positionOffsetZ"] = positionOffset.z;

            variables["scaleOffsetX"] = scaleOffset.x;
            variables["scaleOffsetY"] = scaleOffset.y;
            variables["scaleOffsetZ"] = scaleOffset.z;

            variables["rotationOffsetX"] = rotationOffset.x;
            variables["rotationOffsetY"] = rotationOffset.y;
            variables["rotationOffsetZ"] = rotationOffset.z;

            var transform = runtimeObject.Parent;
            if (!transform)
                return;

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

        public void SetObjectFunctions(Dictionary<string, MathFunction> functions) { }

        #endregion

        public bool TrySetParent(string beatmapObjectToParentTo, bool renderParent = true)
        {
            var dictionary = new Dictionary<string, bool>();
            var beatmapObjects = GameData.Current.beatmapObjects;

            foreach (var obj in beatmapObjects)
                dictionary[obj.id] = CanParent(obj, beatmapObjects);

            dictionary[id] = false;

            var shouldParent = dictionary.TryGetValue(beatmapObjectToParentTo, out bool value) && value;

            if (shouldParent)
            {
                Parent = beatmapObjectToParentTo;
                this.GetParentRuntime()?.UpdatePrefab(this, PrefabObjectContext.PARENT);

                if (renderParent)
                    RTPrefabEditor.inst.RenderPrefabObjectParent(this);
            }

            return shouldParent;
        }

        public bool CanParent(BeatmapObject obj, List<BeatmapObject> beatmapObjects) => true;

        public void UpdateParentChain() => this.GetParentRuntime()?.UpdatePrefab(this, PrefabObjectContext.PARENT);

        public override string ToString() => id;

        #endregion
    }
}
