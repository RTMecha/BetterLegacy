using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data;
using LSFunctions;
using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BasePrefabObject = DataManager.GameData.PrefabObject;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class PrefabObject : BasePrefabObject
    {
        public PrefabObject() : base()
        {
            editorData = new ObjectEditorData();
            events = new List<DataManager.GameData.EventKeyframe>
            {
                new EventKeyframe(),
                new EventKeyframe(),
                new EventKeyframe()
            };
        }

        public PrefabObject(string name, float startTime) : base(name, startTime)
        {
            editorData = new ObjectEditorData();
            events = new List<DataManager.GameData.EventKeyframe>
            {
                new EventKeyframe(),
                new EventKeyframe(),
                new EventKeyframe()
            };
        }

        public PrefabObject(BasePrefabObject prefabObject)
        {
            prefabID = prefabObject.prefabID;
        }

        #region Values

        public float speed = 1f;

        /// <summary>
        /// Gets the prefab reference.
        /// </summary>
        public Prefab Prefab => GameData.Current.prefabs.Find(x => x.ID == prefabID);

        public List<BeatmapObject> expandedObjects = new List<BeatmapObject>();
        public List<BeatmapObject> ExpandedObjects => GameData.Current.beatmapObjects.FindAll(x => x.fromPrefab && x.prefabInstanceID == ID);

        public enum AutoKillType
        {
            Regular,
            StartTimeOffset,
            SongTime
        }

        public AutoKillType autoKillType = AutoKillType.Regular;

        public float autoKillOffset = -1f;

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

        public bool fromModifier;

        public TimelineObject timelineObject;

        public new ObjectEditorData editorData;

        #endregion

        #region Methods

        public static PrefabObject DeepCopy(PrefabObject orig, bool _newID = true)
        {
            var prefabObject = new PrefabObject
            {
                active = orig.active,
                ID = _newID ? LSText.randomString(16) : orig.ID,
                prefabID = orig.prefabID,
                startTime = orig.StartTime,
                repeatCount = orig.repeatCount,
                repeatOffsetTime = orig.repeatOffsetTime,
                editorData = ObjectEditorData.DeepCopy(orig.editorData),
                speed = orig.speed,
                autoKillOffset = orig.autoKillOffset,
                autoKillType = orig.autoKillType,
                parent = orig.parent,
                parentAdditive = orig.parentAdditive,
                parentOffsets = orig.parentOffsets.Copy(),
                parentParallax = orig.parentParallax.Copy(),
                parentType = orig.parentType,
                desync = orig.desync,
            };

            if (prefabObject.events == null)
                prefabObject.events = new List<DataManager.GameData.EventKeyframe>();
            prefabObject.events.Clear();

            if (orig.events != null)
                foreach (var eventKeyframe in orig.events)
                    prefabObject.events.Add(EventKeyframe.DeepCopy((EventKeyframe)eventKeyframe, _newID));

            return prefabObject;
        }

        public static PrefabObject ParseVG(JSONNode jn)
        {
            var prefabObject = new PrefabObject();

            prefabObject.ID = jn["id"];
            prefabObject.prefabID = jn["pid"];
            prefabObject.StartTime = jn["t"] == null ? jn["st"].AsFloat : jn["t"].AsFloat;

            prefabObject.editorData = ObjectEditorData.ParseVG(jn["ed"]);

            prefabObject.events.Clear();

            if (jn["e"] != null)
            {
                try
                {
                    prefabObject.events.Add(new EventKeyframe
                    {
                        eventValues = new float[2]
                        {
                            jn["e"][0]["ev"][0].AsFloat,
                            jn["e"][0]["ev"][1].AsFloat,
                        }
                    });
                }
                catch (System.Exception)
                {
                    prefabObject.events.Add(new EventKeyframe
                    {
                        eventValues = new float[2],
                    });
                }

                try
                {
                    prefabObject.events.Add(new EventKeyframe
                    {
                        eventValues = new float[2]
                        {
                            jn["e"][1]["ev"][0].AsFloat,
                            jn["e"][1]["ev"][1].AsFloat,
                        }
                    });
                }
                catch (System.Exception)
                {
                    prefabObject.events.Add(new EventKeyframe
                    {
                        eventValues = new float[2],
                    });
                }

                try
                {
                    prefabObject.events.Add(new EventKeyframe
                    {
                        eventValues = new float[1] { jn["e"][2]["ev"][0].AsFloat }
                    });
                }
                catch (System.Exception)
                {
                    prefabObject.events.Add(new EventKeyframe
                    {
                        eventValues = new float[1]
                    });
                }
            }
            else
            {
                prefabObject.events.Add(new EventKeyframe(0f, new float[2] { 0f, 0f }, new float[3] { 0f, 0f, 0f }));
                prefabObject.events.Add(new EventKeyframe(0f, new float[2] { 0f, 0f }, new float[3] { 0f, 0f, 0f }));
                prefabObject.events.Add(new EventKeyframe(0f, new float[1] { 0f }, new float[3] { 0f, 0f, 0f }));
            }

            return prefabObject;
        }

        public static PrefabObject Parse(JSONNode jn)
        {
            var prefabObject = new PrefabObject();
            prefabObject.ID = jn["id"] != null ? jn["id"] : LSText.randomString(16);
            prefabObject.prefabID = jn["pid"];
            prefabObject.StartTime = jn["st"].AsFloat;

            if (jn["p"] != null)
                prefabObject.parent = jn["p"];

            if (!string.IsNullOrEmpty(jn["rc"]))
                prefabObject.RepeatCount = jn["rc"].AsInt;

            if (!string.IsNullOrEmpty(jn["ro"]))
                prefabObject.RepeatOffsetTime = jn["ro"].AsFloat;

            if (jn["sp"] != null)
                prefabObject.speed = jn["sp"].AsFloat;

            if (jn["akt"] != null)
                prefabObject.autoKillType = (AutoKillType)jn["akt"].AsInt;

            if (jn["ako"] != null)
                prefabObject.autoKillOffset = jn["ako"].AsFloat;

            if (jn["ed"] != null)
                prefabObject.editorData = ObjectEditorData.Parse(jn["ed"]);

            prefabObject.events.Clear();

            if (jn["e"] != null)
            {
                if (jn["e"]["pos"] != null)
                {
                    var kf = new EventKeyframe();
                    var jnpos = jn["e"]["pos"];

                    kf.SetEventValues(new float[]
                    {
                        jnpos["x"].AsFloat,
                        jnpos["y"].AsFloat
                    });
                    kf.random = jnpos["r"].AsInt;
                    kf.SetEventRandomValues(new float[]
                    {
                        jnpos["rx"].AsFloat,
                        jnpos["ry"].AsFloat,
                        jnpos["rz"].AsFloat
                    });
                    kf.active = false;
                    prefabObject.events.Add(kf);
                }
                else
                {
                    prefabObject.events.Add(new EventKeyframe(new float[2] { 0f, 0f }, new float[3] { 0f, 0f, 0f }));
                }
                if (jn["e"]["sca"] != null)
                {
                    var kf = new EventKeyframe();
                    var jnsca = jn["e"]["sca"];
                    kf.SetEventValues(new float[]
                    {
                        jnsca["x"].AsFloat,
                        jnsca["y"].AsFloat
                    });
                    kf.random = jnsca["r"].AsInt;
                    kf.SetEventRandomValues(new float[]
                    {
                        jnsca["rx"].AsFloat,
                        jnsca["ry"].AsFloat,
                        jnsca["rz"].AsFloat
                    });
                    kf.active = false;
                    prefabObject.events.Add(kf);
                }
                else
                {
                    prefabObject.events.Add(new EventKeyframe(new float[2] { 1f, 1f }, new float[3] { 0f, 0f, 0f }));
                }
                if (jn["e"]["rot"] != null)
                {
                    var kf = new EventKeyframe();
                    var jnrot = jn["e"]["rot"];
                    kf.SetEventValues(new float[]
                    {
                        jnrot["x"].AsFloat
                    });
                    kf.random = jnrot["r"].AsInt;
                    kf.SetEventRandomValues(new float[]
                    {
                        jnrot["rx"].AsFloat,
                        0f,
                        jnrot["rz"].AsFloat
                    });
                    kf.active = false;
                    prefabObject.events.Add(kf);
                }
                else
                {
                    prefabObject.events.Add(new EventKeyframe(new float[1] { 0f }, new float[3] { 0f, 0f, 0f }));
                }
            }
            else
            {
                prefabObject.events = new List<DataManager.GameData.EventKeyframe>()
                {
                    new EventKeyframe(new float[2] { 0f, 0f }, new float[3] { 0f, 0f, 0f }),
                    new EventKeyframe(new float[2] { 1f, 1f }, new float[3] { 0f, 0f, 0f }),
                    new EventKeyframe(new float[1] { 0f }, new float[3] { 0f, 0f, 0f }),
                };
            }
            return prefabObject;
        }

        public JSONNode ToJSONVG()
        {
            var jn = JSON.Parse("{}");

            jn["id"] = ID;
            jn["pid"] = prefabID;

            jn["ed"] = ((ObjectEditorData)editorData).ToJSONVG();

            jn["e"][0]["ct"] = "Linear";
            jn["e"][0]["ev"][0] = events[0].eventValues[0];
            jn["e"][0]["ev"][1] = events[0].eventValues[1];

            jn["e"][1]["ct"] = "Linear";
            jn["e"][1]["ev"][0] = events[1].eventValues[0];
            jn["e"][1]["ev"][1] = events[1].eventValues[1];

            jn["e"][2]["ct"] = "Linear";
            jn["e"][2]["ev"][0] = events[2].eventValues[0];

            jn["t"] = StartTime;

            return jn;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["id"] = ID;
            jn["pid"] = prefabID;
            jn["st"] = StartTime.ToString();

            if (speed != 1f)
                jn["sp"] = speed.ToString();

            if (parentType != "111")
                jn["pt"] = parentType;

            if (parentOffsets.Any(x => x != 0f))
            {
                for (int i = 0; i < parentOffsets.Length; i++)
                    jn["po"][i] = parentOffsets[i].ToString();
            }

            if (parentAdditive != "000")
                jn["pa"] = parentAdditive;

            if (parentParallax.Any(x => x != 1f))
            {
                for (int i = 0; i < parentParallax.Length; i++)
                    jn["ps"][i] = parentParallax[i].ToString();
            }

            if (!string.IsNullOrEmpty(parent))
                jn["p"] = parent;

            if (autoKillType != AutoKillType.Regular)
            {
                jn["akt"] = ((int)autoKillType).ToString();

                if (autoKillOffset != -1f)
                    jn["ako"] = autoKillOffset.ToString();
            }

            if (RepeatCount > 0)
                jn["rc"] = RepeatCount.ToString();
            if (RepeatOffsetTime > 0f)
                jn["ro"] = RepeatOffsetTime.ToString();

            if (editorData.locked)
                jn["ed"]["locked"] = editorData.locked.ToString();
            if (editorData.collapse)
                jn["ed"]["shrink"] = editorData.collapse.ToString();

            if (editorData.layer != 0)
                jn["ed"]["layer"] = editorData.layer.ToString();
            if (editorData.Bin != 0)
                jn["ed"]["bin"] = editorData.Bin.ToString();

            jn["e"]["pos"]["x"] = events[0].eventValues[0].ToString();
            jn["e"]["pos"]["y"] = events[0].eventValues[1].ToString();
            if (events[0].random != 0)
            {
                jn["e"]["pos"]["r"] = events[0].random.ToString();
                jn["e"]["pos"]["rx"] = events[0].eventRandomValues[0].ToString();
                jn["e"]["pos"]["ry"] = events[0].eventRandomValues[1].ToString();
                jn["e"]["pos"]["rz"] = events[0].eventRandomValues[2].ToString();
            }

            jn["e"]["sca"]["x"] = events[1].eventValues[0].ToString();
            jn["e"]["sca"]["y"] = events[1].eventValues[1].ToString();
            if (events[1].random != 0)
            {
                jn["e"]["sca"]["r"] = events[1].random.ToString();
                jn["e"]["sca"]["rx"] = events[1].eventRandomValues[0].ToString();
                jn["e"]["sca"]["ry"] = events[1].eventRandomValues[1].ToString();
                jn["e"]["sca"]["rz"] = events[1].eventRandomValues[2].ToString();
            }

            jn["e"]["rot"]["x"] = events[2].eventValues[0].ToString();
            if (events[1].random != 0)
            {
                jn["e"]["rot"]["r"] = events[2].random.ToString();
                jn["e"]["rot"]["rx"] = events[2].eventRandomValues[0].ToString();
                jn["e"]["rot"]["rz"] = events[2].eventRandomValues[2].ToString();
            }

            return jn;
        }

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
            var prefab = Prefab;
            if (collapse && editorData.collapse || !prefab)
                return 0.2f;

            float time = prefab.objects.Select(x => x.StartTime).Min(x => x);
            return prefab.objects.Max(x => x.StartTime + (x as BeatmapObject).GetObjectLifeLength(collapse: true) - time);
        }

        #endregion

        #region Operators

        public static implicit operator bool(PrefabObject exists) => exists != null;

        public override bool Equals(object obj) => obj is PrefabObject && ID == (obj as PrefabObject).ID;

        public override string ToString() => ID;

        #endregion
    }
}
