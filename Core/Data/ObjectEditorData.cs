﻿using SimpleJSON;
using UnityEngine;
using BaseEditorData = DataManager.GameData.BeatmapObject.EditorData;

namespace BetterLegacy.Core.Data
{
    public class ObjectEditorData : BaseEditorData
    {
        public ObjectEditorData()
        {

        }

        public ObjectEditorData(int bin, int layer, bool collapse, bool locked)
        {
            Bin = bin;
            Layer = layer;
            this.collapse = collapse;
            this.locked = locked;
        }

        public new int Layer
        {
            get => Mathf.Clamp(layer, 0, int.MaxValue);
            set => layer = Mathf.Clamp(value, 0, int.MaxValue);
        }

        public int Index { get; set; } = -1;

        #region Methods

        public static ObjectEditorData DeepCopy(ObjectEditorData orig) => new ObjectEditorData
        {
            Bin = orig.Bin,
            Layer = orig.Layer,
            collapse = orig.collapse,
            locked = orig.locked,
            Index = orig.Index,
        };

        public static ObjectEditorData ParseVG(JSONNode jn) => new ObjectEditorData
        {
            Bin = jn["b"] == null ? 0 : jn["b"].AsInt,
            layer = jn["l"] == null ? 0 : Mathf.Clamp(jn["l"].AsInt, 0, int.MaxValue),
            locked = jn["lk"] == null ? false : jn["lk"].AsBool,
            collapse = jn["co"] == null ? false : jn["co"].AsBool,
        };

        public static ObjectEditorData Parse(JSONNode jn) => new ObjectEditorData
        {
            Bin = jn["bin"] == null ? 0 : jn["bin"].AsInt,
            layer = jn["layer"] == null ? 0 : Mathf.Clamp(jn["layer"].AsInt, 0, int.MaxValue),
            collapse = jn["shrink"] == null ? jn["collapse"] == null ? false : jn["collapse"].AsBool : jn["shrink"].AsBool,
            locked = jn["locked"] == null ? false : jn["locked"].AsBool,
            Index = jn["index"] == null ? -1 : jn["index"].AsInt,
        };

        public JSONNode ToJSONVG()
        {
            var jn = JSON.Parse("{}");

            jn["lk"] = locked;
            jn["co"] = collapse;
            jn["b"] = Bin;
            jn["l"] = layer;

            return jn;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["bin"] = Bin.ToString();
            jn["layer"] = layer.ToString();
            if (collapse)
                jn["collapse"] = collapse.ToString();
            if (locked)
                jn["locked"] = locked.ToString();

            if (Index != -1)
                jn["index"] = Index.ToString();

            return jn;
        }

        #endregion
    }
}
