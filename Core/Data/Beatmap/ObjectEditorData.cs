using UnityEngine;

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
        
        public ObjectEditorData(int bin, int layer, bool collapse, bool locked, bool selectable, bool hidden, string color, string selectedColor, string textColor, string markColor)
        {
            Bin = bin;
            Layer = layer;
            this.collapse = collapse;
            this.locked = locked;
            this.selectable = selectable;
            this.hidden = hidden;
            this.color = color;
            this.selectedColor = selectedColor;
            this.textColor = textColor;
            this.markColor = markColor;
        }

        #region Values

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

        public string color;

        public string selectedColor;

        public string textColor;

        public string markColor;

        /// <summary>
        /// If the editor data should serialize to JSON.
        /// </summary>
        public bool ShouldSerialize => Bin != 0 || Layer != 0 || collapse || locked || !selectable || hidden || !string.IsNullOrEmpty(color) || !string.IsNullOrEmpty(selectedColor) || !string.IsNullOrEmpty(textColor) || !string.IsNullOrEmpty(markColor);

        #endregion

        #region Constants

        public const string RED_GREEN = "FFE904";

        public const string RED_BLUE = "952BFF";

        public const string GREEN_BLUE = "34E7FF";

        public const string RED = "EE153F";

        public const string GREEN = "00D36E";

        public const string BLUE = "265AEE";

        #endregion

        #region Methods

        public static ObjectEditorData DeepCopy(ObjectEditorData orig) => new ObjectEditorData(orig.Bin, orig.Layer, orig.collapse, orig.locked, orig.selectable, orig.hidden, orig.color, orig.selectedColor, orig.textColor, orig.markColor);

        public static ObjectEditorData ParseVG(JSONNode jn)
        {
            var objectEditorData = new ObjectEditorData(jn["b"].AsInt, Mathf.Clamp(jn["l"].AsInt, 0, int.MaxValue), jn["lk"].AsBool, jn["co"].AsBool);

            var tc = jn["tc"];
            if (tc != null)
                objectEditorData.textColor = GetDefaultColorVG(tc["r"].AsBool, tc["g"].AsBool, tc["b"].AsBool);
            var bgc = jn["bgc"];
            if (bgc != null)
                objectEditorData.color = GetDefaultColorVG(bgc["r"].AsBool, bgc["g"].AsBool, bgc["b"].AsBool);

            return objectEditorData;
        }

        public static ObjectEditorData Parse(JSONNode jn) => new ObjectEditorData(jn["bin"].AsInt, Mathf.Clamp(jn["layer"].AsInt, 0, int.MaxValue), jn["shrink"] == null ? jn["collapse"].AsBool : jn["shrink"].AsBool, jn["locked"].AsBool, jn["select"] == null || jn["select"].AsBool, jn["hide"].AsBool, jn["col"], jn["selcol"], jn["texcol"], jn["markcol"]);

        public JSONNode ToJSONVG()
        {
            var jn = JSON.Parse("{}");

            jn["lk"] = locked;
            jn["co"] = collapse;
            jn["b"] = Bin;
            jn["l"] = Layer;

            if (!string.IsNullOrEmpty(textColor))
            {
                var tc = HexToColorBool(textColor);
                if (tc != null)
                    jn["tc"] = tc;
            }
            
            if (!string.IsNullOrEmpty(color))
            {
                var bgc = HexToColorBool(color);
                if (bgc != null)
                    jn["bgc"] = bgc;
            }

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

            if (!string.IsNullOrEmpty(color))
                jn["col"] = color;
            if (!string.IsNullOrEmpty(selectedColor))
                jn["selcol"] = selectedColor;
            if (!string.IsNullOrEmpty(textColor))
                jn["texcol"] = textColor;
            if (!string.IsNullOrEmpty(markColor))
                jn["markcol"] = markColor;

            return jn;
        }

        static string GetDefaultColorVG(bool r, bool g, bool b) => r && g && b ? RTColors.WHITE_HEX_CODE : r && g ? RED_GREEN : r && b ? RED_BLUE : g && b ? GREEN_BLUE : r ? RED : g ? GREEN : b ? BLUE : RTColors.WHITE_HEX_CODE;

        static JSONNode HexToColorBool(string color)
        {
            var jn = Parser.NewJSONObject();

            switch (color)
            {
                case RED_GREEN: {
                        jn["r"] = true;
                        jn["g"] = true;
                        jn["b"] = false;
                        break;
                    }
                case RED_BLUE: {
                        jn["r"] = true;
                        jn["g"] = false;
                        jn["b"] = true;
                        break;
                    }
                case GREEN_BLUE: {
                        jn["r"] = false;
                        jn["g"] = true;
                        jn["b"] = true;
                        break;
                    }
                case RED: {
                        jn["r"] = true;
                        jn["g"] = false;
                        jn["b"] = false;
                        break;
                    }
                case GREEN: {
                        jn["r"] = false;
                        jn["g"] = true;
                        jn["b"] = false;
                        break;
                    }
                case BLUE: {
                        jn["r"] = false;
                        jn["g"] = false;
                        jn["b"] = true;
                        break;
                    }
                default: {
                        return null;
                    }
            }

            return jn;
        }

        #endregion
    }
}
