using BetterLegacy.Editor.Managers;
using SimpleJSON;
using UnityEngine;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Contains info about a PA objects' layer, bin, etc.
    /// </summary>
    public class ObjectEditorData : Exists
    {
        public ObjectEditorData() {  }

        public ObjectEditorData(int bin, int layer, bool collapse, bool locked)
        {
            Bin = bin;
            Layer = layer;
            this.collapse = collapse;
            this.locked = locked;
        }

        int bin;
        public int Bin
        {
            get => bin;
            set => bin = value;
        }

        int layer;
        public int Layer
        {
            get => Mathf.Clamp(layer, 0, int.MaxValue);
            set => layer = Mathf.Clamp(value, 0, int.MaxValue);
        }

        public bool collapse;

        public bool locked;

        #region Methods

        public static ObjectEditorData DeepCopy(ObjectEditorData orig) => new ObjectEditorData(orig.Bin, orig.Layer, orig.collapse, orig.locked);

        public static ObjectEditorData ParseVG(JSONNode jn) => new ObjectEditorData(jn["b"].AsInt, Mathf.Clamp(jn["l"].AsInt, 0, int.MaxValue), jn["lk"].AsBool, jn["co"].AsBool);

        public static ObjectEditorData Parse(JSONNode jn) => new ObjectEditorData(jn["bin"].AsInt, Mathf.Clamp(jn["layer"].AsInt, 0, int.MaxValue), jn["shrink"] == null ? jn["collapse"].AsBool : jn["shrink"].AsBool, jn["locked"].AsBool);

        public JSONNode ToJSONVG()
        {
            var jn = JSON.Parse("{}");

            jn["lk"] = locked;
            jn["co"] = collapse;
            jn["b"] = Bin;
            jn["l"] = Layer;
             
            return jn;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["bin"] = Bin;
            jn["layer"] = Layer;
            if (collapse)
                jn["collapse"] = collapse;
            if (locked)
                jn["locked"] = locked;

            return jn;
        }

        #endregion
    }
}
