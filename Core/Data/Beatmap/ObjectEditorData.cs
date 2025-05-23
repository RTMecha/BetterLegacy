﻿using UnityEngine;

using SimpleJSON;

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
        
        public ObjectEditorData(int bin, int layer, bool collapse, bool locked, bool selectable, bool hidden)
        {
            Bin = bin;
            Layer = layer;
            this.collapse = collapse;
            this.locked = locked;
            this.selectable = selectable;
            this.hidden = hidden;
        }

        int bin;
        /// <summary>
        /// Row the timeline object is at.
        /// </summary>
        public int Bin
        {
            get => bin;
            set => bin = value;
        }

        int layer;
        /// <summary>
        /// Layer the timeline object appears on.
        /// </summary>
        public int Layer
        {
            get => Mathf.Clamp(layer, 0, int.MaxValue);
            set => layer = Mathf.Clamp(value, 0, int.MaxValue);
        }

        /// <summary>
        /// If the timeline object should be collapsed.
        /// </summary>
        public bool collapse;

        /// <summary>
        /// If the start time should be locked.
        /// </summary>
        public bool locked;

        /// <summary>
        /// If the object is highlightable and selectable in the editor preview window.
        /// </summary>
        public bool selectable = true;

        /// <summary>
        /// If the object is hidden in the editor.
        /// </summary>
        public bool hidden = false;

        /// <summary>
        /// If the editor data should serialize to JSON.
        /// </summary>
        public bool ShouldSerialize => Bin != 0 || Layer != 0 || collapse || locked || !selectable || hidden;

        #region Methods

        public static ObjectEditorData DeepCopy(ObjectEditorData orig) => new ObjectEditorData(orig.Bin, orig.Layer, orig.collapse, orig.locked, orig.selectable, orig.hidden);

        public static ObjectEditorData ParseVG(JSONNode jn) => new ObjectEditorData(jn["b"].AsInt, Mathf.Clamp(jn["l"].AsInt, 0, int.MaxValue), jn["lk"].AsBool, jn["co"].AsBool);

        public static ObjectEditorData Parse(JSONNode jn) => new ObjectEditorData(jn["bin"].AsInt, Mathf.Clamp(jn["layer"].AsInt, 0, int.MaxValue), jn["shrink"] == null ? jn["collapse"].AsBool : jn["shrink"].AsBool, jn["locked"].AsBool, jn["select"] != null ? jn["select"].AsBool : true, jn["hide"].AsBool);

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

            if (Bin != 0)
                jn["bin"] = Bin;
            if (Layer != 0)
                jn["layer"] = Layer;
            if (collapse)
                jn["collapse"] = collapse;
            if (locked)
                jn["locked"] = locked;
            if (!selectable)
                jn["select"] = selectable;
            if (hidden)
                jn["hide"] = hidden;

            return jn;
        }

        #endregion
    }
}
