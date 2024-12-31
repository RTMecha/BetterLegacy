using SimpleJSON;
using UnityEngine;
using BaseEditorData = DataManager.GameData.BeatmapObject.EditorData;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Contains info about a PA objects' layer, bin, etc.
    /// </summary>
    public class ObjectEditorData : BaseEditorData
    {
        public ObjectEditorData() {  }

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

            jn["bin"] = Bin.ToString();
            jn["layer"] = Layer.ToString();
            if (collapse)
                jn["collapse"] = collapse.ToString();
            if (locked)
                jn["locked"] = locked.ToString();

            return jn;
        }

        #endregion
    }
}
