using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// An instance of a <see cref="Prefab"/> that spawns all objects contained in the Prefab.
    /// </summary>
    public class PrefabObject : PAObject<PrefabObject>, ILifetime<PrefabAutoKillType>, ITransformable, IParentable, IModifyable<PrefabObject>, IEditable
    {
        public PrefabObject() : base()
        {
            editorData.Bin = 0;
            editorData.Layer = 0;

            events = new List<EventKeyframe>
            {
                new EventKeyframe(),
                new EventKeyframe(),
                new EventKeyframe()
            };
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

        /// <summary>
        /// Transform offsets.
        /// </summary>
        public List<EventKeyframe> events = new List<EventKeyframe>();

        #region Parent

        /// <summary>
        /// ID of the object to parent all spawned base objects to.
        /// </summary>
        public string parent;

        public string Parent { get => parent; set => parent = value; }

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

        public PrefabAutoKillType AutoKillType { get => autoKillType; set => autoKillType = value; }

        public float autoKillOffset = -1f;

        public float AutoKillOffset { get => autoKillOffset; set => autoKillOffset = value; }

        public bool Alive => false;

        public float SpawnDuration => 0f;

        #endregion

        #region Modifiers

        /// <summary>
        /// The tags used to identify a group of objects or object properties.
        /// </summary>
        public List<string> tags = new List<string>();

        public List<string> Tags { get => tags; set => tags = value; }

        /// <summary>
        /// Modifiers the object contains.
        /// </summary>
        public List<Modifier<PrefabObject>> modifiers = new List<Modifier<PrefabObject>>();

        public List<Modifier<PrefabObject>> Modifiers { get => modifiers; set => modifiers = value; }

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
                var startTime = ignoreLifespan ? 0f : StartTime;
                var killTime = ignoreLifespan ? SoundManager.inst.MusicLength : StartTime + SpawnDuration;
                return AudioManager.inst.CurrentAudioSource.time >= startTime && AudioManager.inst.CurrentAudioSource.time <= killTime;
            }
        }

        #endregion

        #region Runtime

        /// <summary>
        /// Cached transform.
        /// </summary>
        public ObjectTransform? cachedTransform;

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

        #endregion

        #endregion

        #region Methods

        public override void CopyData(PrefabObject orig, bool newID = true)
        {
            id = newID ? LSText.randomString(16) : orig.id;
            prefabID = orig.prefabID;
            startTime = orig.StartTime;
            repeatCount = orig.repeatCount;
            repeatOffsetTime = orig.repeatOffsetTime;
            editorData = ObjectEditorData.DeepCopy(orig.editorData);
            speed = orig.Speed;
            autoKillOffset = orig.autoKillOffset;
            autoKillType = orig.autoKillType;
            parent = orig.parent;
            parentAdditive = orig.parentAdditive;
            parentOffsets = orig.parentOffsets.Copy();
            parentParallax = orig.parentParallax.Copy();
            parentType = orig.parentType;
            desync = orig.desync;

            if (events == null)
                events = new List<EventKeyframe>();
            events.Clear();

            if (orig.events != null)
                foreach (var eventKeyframe in orig.events)
                    events.Add(eventKeyframe.Copy(newID));

            tags = !orig.tags.IsEmpty() ? orig.tags.Clone() : new List<string>();
            ignoreLifespan = orig.ignoreLifespan;
            orderModifiers = orig.orderModifiers;
            modifiers = !orig.modifiers.IsEmpty() ? orig.modifiers.Select(x => x.Copy(this)).ToList() : new List<Modifier<PrefabObject>>();
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            id = jn["id"];
            prefabID = jn["pid"];
            StartTime = jn["t"] == null ? jn["st"].AsFloat : jn["t"].AsFloat;

            editorData = ObjectEditorData.ParseVG(jn["ed"]);

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
                        values = new float[2],
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
            {
                events.Add(new EventKeyframe(0f, new float[2] { 0f, 0f }, new float[3] { 0f, 0f, 0f }));
                events.Add(new EventKeyframe(0f, new float[2] { 0f, 0f }, new float[3] { 0f, 0f, 0f }));
                events.Add(new EventKeyframe(0f, new float[1] { 0f }, new float[3] { 0f, 0f, 0f }));
            }
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? LSText.randomString(16);
            prefabID = jn["pid"];
            StartTime = jn["st"].AsFloat;

            this.ReadParentJSON(jn);

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
                editorData = ObjectEditorData.Parse(jn["ed"]);

            expanded = jn["exp"].AsBool;

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
            {
                events = new List<EventKeyframe>()
                {
                    new EventKeyframe(new float[2] { 0f, 0f }, new float[3] { 0f, 0f, 0f }),
                    new EventKeyframe(new float[2] { 1f, 1f }, new float[3] { 0f, 0f, 0f }),
                    new EventKeyframe(new float[1] { 0f }, new float[3] { 0f, 0f, 0f }),
                };
            }

            if (jn["iglif"] != null)
                ignoreLifespan = jn["iglif"].AsBool;

            if (jn["ordmod"] != null)
                orderModifiers = jn["ordmod"].AsBool;

            for (int i = 0; i < jn["modifiers"].Count; i++)
            {
                var modifier = Modifier<PrefabObject>.Parse(jn["modifiers"][i], this);
                if (ModifiersHelper.VerifyModifier(modifier, ModifiersManager.defaultPrefabObjectModifiers))
                    modifiers.Add(modifier);
            }
        }

        public override JSONNode ToJSONVG()
        {
            var jn = JSON.Parse("{}");

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
            var jn = JSON.Parse("{}");

            jn["id"] = id;
            jn["pid"] = prefabID;
            jn["st"] = StartTime;

            if (Speed != 1f)
                jn["sp"] = Speed;

            this.WriteParentJSON(jn);

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

            if (editorData.locked)
                jn["ed"]["locked"] = editorData.locked;
            if (editorData.collapse)
                jn["ed"]["shrink"] = editorData.collapse;

            if (editorData.Layer != 0)
                jn["ed"]["layer"] = editorData.Layer;
            if (editorData.Bin != 0)
                jn["ed"]["bin"] = editorData.Bin;

            if (expanded)
                jn["exp"] = expanded;

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

            if (ignoreLifespan)
                jn["iglif"] = ignoreLifespan;
            if (orderModifiers)
                jn["ordmod"] = orderModifiers;
            for (int i = 0; i < modifiers.Count; i++)
                jn["modifiers"][i] = modifiers[i].ToJSON();

            return jn;
        }

        /// <summary>
        /// Gets the prefab reference.
        /// </summary>
        public Prefab GetPrefab() => GameData.Current.prefabs.Find(x => x.id == prefabID);

        public void SetParentAdditive(int index, bool additive)
        {
            var stringBuilder = new StringBuilder(parentAdditive);
            stringBuilder[index] = additive ? '1' : '0';
            parentAdditive = stringBuilder.ToString();
            CoreHelper.Log($"Set parent additive: {parentAdditive}");
        }

        public bool GetParentType(int index) => parentType[index] == '1';

        public void SetParentType(int index, bool active)
        {
            var stringBuilder = new StringBuilder(parentType);
            stringBuilder[index] = active ? '1' : '0';
            parentType = stringBuilder.ToString();
            Debug.Log("Set Parent Type: " + parentType);
        }
        public void SetParentOffset(int index, float value)
        {
            if (index >= 0 && index < parentOffsets.Length)
                parentOffsets[index] = value;
        }

        public float GetObjectLifeLength(float offset = 0.0f, bool noAutokill = false, bool collapse = false)
        {
            if (collapse && editorData.collapse)
                return 0.2f;

            var prefab = GetPrefab();
            if (!prefab)
                return 0.2f;

            float length = 0.2f;

            if (!prefab.beatmapObjects.IsEmpty())
            {
                var time = prefab.beatmapObjects.Min(x => x.StartTime);
                length = prefab.beatmapObjects.Max(x => x.StartTime + x.GetObjectLifeLength(collapse: true) - time);
            }

            if (!prefab.backgroundObjects.IsEmpty())
            {
                var time = prefab.backgroundObjects.Min(x => x.StartTime);
                var l = prefab.backgroundObjects.Max(x => x.StartTime + x.GetObjectLifeLength(collapse: true) - time);
                if (l > length)
                    length = l;
            }

            return length;
        }
        
        public void ResetOffsets()
        {
            reactivePositionOffset = Vector3.zero;
            reactiveScaleOffset = Vector3.zero;
            reactiveRotationOffset = 0f;
            positionOffset = Vector3.zero;
            scaleOffset = Vector3.zero;
            rotationOffset = Vector3.zero;
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

        public Vector3 GetFullPosition() => positionOffset + new Vector3(events[0].values[0], events[0].values[1]);

        public Vector3 GetFullScale()
        {
            var scale = scaleOffset;
            scale.x *= events[1].values[0];
            scale.y *= events[1].values[1];
            return scale.x != 0f && scale.y != 0f ? scale : Vector3.one;
        }

        public Vector3 GetFullRotation() => rotationOffset + new Vector3(0f, 0f, events[2].values[0]);

        /// <summary>
        /// Gets the transform offsets from the Prefab Object.
        /// </summary>
        /// <param name="prefabObject">Prefab Object to get the transform offsets from.</param>
        /// <returns>Returns a struct representing the objects' transform values.</returns>
        public ObjectTransform GetTransformOffset()
        {
            if (cachedTransform != null && cachedTransform.HasValue)
                return cachedTransform.Value;

            var transform = ObjectTransform.Default;

            bool hasPosX = events.Count > 0 && events[0] && events[0].values.Length > 0;
            bool hasPosY = events.Count > 0 && events[0] && events[0].values.Length > 1;

            bool hasScaX = events.Count > 1 && events[1] && events[1].values.Length > 0;
            bool hasScaY = events.Count > 1 && events[1] && events[1].values.Length > 1;

            bool hasRot = events.Count > 2 && events[2] && events[2].values.Length > 0;

            transform.position = new Vector3(hasPosX ? events[0].values[0] : 0f, hasPosY ? events[0].values[1] : 0f, 0f);
            transform.scale = new Vector2(hasScaX ? events[1].values[0] : 1f, hasScaY ? events[1].values[1] : 1f);
            transform.rotation = hasRot ? events[2].values[0] : 0f;

            try
            {
                if (events[0].random != 0)
                    transform.position = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(events[0]);
                if (events[1].random != 0)
                    transform.scale = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(events[1]);
                if (events[2].random != 0)
                    transform.rotation = RandomHelper.KeyframeRandomizer.RandomizeFloatKeyframe(events[2]);
            }
            catch (System.Exception ex)
            {
                CoreHelper.LogError($"Prefab Randomization error.\nException: {ex}");
            }

            transform.scale = transform.scale.x != 0f && transform.scale.y != 0f ? transform.scale : Vector3.one;
            cachedTransform = transform;

            return transform;
        }

        #endregion

        #region Operators

        public override bool Equals(object obj) => obj is PrefabObject paObj && id == paObj.id;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => id;

        #endregion
    }
}
