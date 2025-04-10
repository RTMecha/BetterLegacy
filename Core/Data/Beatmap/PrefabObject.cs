using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// An instance of a <see cref="Prefab"/>.
    /// </summary>
    public class PrefabObject : PAObject<PrefabObject>
    {
        public PrefabObject()
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

        public PrefabObject(string prefabID, float startTime) : this()
        {
			this.prefabID = prefabID;
			this.startTime = startTime;
        }

        #region Values

        public string prefabID = "";

        public List<EventKeyframe> events = new List<EventKeyframe>();

        public ObjectEditorData editorData = new ObjectEditorData();

        #region Parent

        public string parent;

        public string parentType = "111";

        public float[] parentOffsets = new float[3]
        {
            0f,
            0f,
            0f
        };

        public float[] parentParallax = new float[3]
        {
            1f,
            1f,
            1f
        };

        public string parentAdditive = "000";

        public bool desync;

        #endregion

        #region Timing

        /// <summary>
        /// The max speed of a prefab object.
        /// </summary>
        public const float MAX_PREFAB_OBJECT_SPEED = 1000f;

        float startTime;
        public float StartTime
        {
            get => startTime;
            set => startTime = value;
        }

        int repeatCount;
        public int RepeatCount
        {
            get => repeatCount;
            set => repeatCount = Mathf.Clamp(value, 0, 1000);
        }

        float repeatOffsetTime;
        public float RepeatOffsetTime
        {
            get => repeatOffsetTime;
            set => repeatOffsetTime = Mathf.Clamp(value, 0f, 60f);
        }

        public float speed = 1f;
        public float Speed
        {
            get => Mathf.Clamp(speed, 0.01f, MAX_PREFAB_OBJECT_SPEED);
            set => speed = Mathf.Clamp(value, 0.01f, MAX_PREFAB_OBJECT_SPEED);
        }

        public enum AutoKillType
        {
            Regular,
            StartTimeOffset,
            SongTime
        }

        public AutoKillType autoKillType = AutoKillType.Regular;

        public float autoKillOffset = -1f;

        #endregion

        #region References

        public bool fromModifier;

        public List<BeatmapObject> expandedObjects = new List<BeatmapObject>();

        public List<BeatmapObject> ExpandedObjects => GameData.Current.beatmapObjects.FindAll(x => x.fromPrefab && x.prefabInstanceID == id);

        public TimelineObject timelineObject;

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

            if (jn["p"] != null)
                parent = jn["p"];

            if (jn["rc"] != null)
                RepeatCount = jn["rc"].AsInt;

            if (jn["ro"] != null)
                RepeatOffsetTime = jn["ro"].AsFloat;

            if (jn["sp"] != null)
                Speed = jn["sp"].AsFloat;

            if (jn["akt"] != null)
                autoKillType = (AutoKillType)jn["akt"].AsInt;

            if (jn["ako"] != null)
                autoKillOffset = jn["ako"].AsFloat;

            if (jn["ed"] != null)
                editorData = ObjectEditorData.Parse(jn["ed"]);

            events.Clear();

            if (jn["e"] != null)
            {
                if (jn["e"]["pos"] != null)
                {
                    var kf = new EventKeyframe();
                    var jnpos = jn["e"]["pos"];

                    kf.SetEventValues(jnpos["x"].AsFloat, jnpos["y"].AsFloat);
                    kf.random = jnpos["r"].AsInt;
                    kf.SetEventRandomValues(jnpos["rx"].AsFloat, jnpos["ry"].AsFloat, jnpos["rz"].AsFloat);
                    events.Add(kf);
                }
                else
                    events.Add(new EventKeyframe(new float[2] { 0f, 0f }, new float[3] { 0f, 0f, 0f }));

                if (jn["e"]["sca"] != null)
                {
                    var kf = new EventKeyframe();
                    var jnsca = jn["e"]["sca"];
                    kf.SetEventValues(jnsca["x"].AsFloat, jnsca["y"].AsFloat);
                    kf.random = jnsca["r"].AsInt;
                    kf.SetEventRandomValues(jnsca["rx"].AsFloat, jnsca["ry"].AsFloat, jnsca["rz"].AsFloat);
                    events.Add(kf);
                }
                else
                    events.Add(new EventKeyframe(new float[2] { 1f, 1f }, new float[3] { 0f, 0f, 0f }));

                if (jn["e"]["rot"] != null)
                {
                    var kf = new EventKeyframe();
                    var jnrot = jn["e"]["rot"];
                    kf.SetEventValues(jnrot["x"].AsFloat);
                    kf.random = jnrot["r"].AsInt;
                    kf.SetEventRandomValues(jnrot["rx"].AsFloat, 0f, jnrot["rz"].AsFloat);
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

            if (parentType != "111")
                jn["pt"] = parentType;

            if (parentOffsets.Any(x => x != 0f))
            {
                for (int i = 0; i < parentOffsets.Length; i++)
                    jn["po"][i] = parentOffsets[i];
            }

            if (parentAdditive != "000")
                jn["pa"] = parentAdditive;

            if (parentParallax.Any(x => x != 1f))
            {
                for (int i = 0; i < parentParallax.Length; i++)
                    jn["ps"][i] = parentParallax[i];
            }

            if (!string.IsNullOrEmpty(parent))
                jn["p"] = parent;

            if (autoKillType != AutoKillType.Regular)
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
            if (events[1].random != 0)
            {
                jn["e"]["rot"]["r"] = events[2].random;
                jn["e"]["rot"]["rx"] = events[2].randomValues[0];
                jn["e"]["rot"]["rz"] = events[2].randomValues[2];
            }

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

        /// <summary>
        /// Collapse is now taken into consideration for both the prefab and all objects inside.
        /// </summary>
        /// <param name="prefab">The Prefab reference</param>
        /// <param name="collapse">If collapse should be taken into consideration.</param>
        /// <returns></returns>
        public float GetPrefabLifeLength(bool collapse = false)
        {
            var prefab = GetPrefab();
            if (collapse && editorData.collapse || !prefab)
                return 0.2f;

            float time = prefab.beatmapObjects.Select(x => x.StartTime).Min(x => x);
            return prefab.beatmapObjects.Max(x => x.StartTime + (x as BeatmapObject).GetObjectLifeLength(collapse: true) - time);
        }

        #endregion

        #region Operators

        public override bool Equals(object obj) => obj is PrefabObject paObj && id == paObj.id;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => id;

        #endregion
    }
}
