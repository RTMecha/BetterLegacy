using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Contains info about a PA objects' layer, bin, etc.
    /// </summary>
    public class ObjectEditorData : PAObject<ObjectEditorData>
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

        public List<CustomUIDisplay> displays = new List<CustomUIDisplay>();

        /// <summary>
        /// If the editor data should serialize to JSON.
        /// </summary>
        public bool ShouldSerialize =>
            Bin != 0 || Layer != 0 ||
            collapse || locked || !selectable || hidden ||
            !string.IsNullOrEmpty(color) || !string.IsNullOrEmpty(selectedColor) || !string.IsNullOrEmpty(textColor) || !string.IsNullOrEmpty(markColor) ||
            !displays.IsEmpty();

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

        public override void CopyData(ObjectEditorData orig, bool newID = true)
        {
            Bin = orig.Bin;
            Layer = orig.Layer;
            collapse = orig.collapse;
            locked = orig.locked;
            selectable = orig.selectable;
            hidden = orig.hidden;
            color = orig.color;
            selectedColor = orig.selectedColor;
            textColor = orig.textColor;
            markColor = orig.markColor;
            displays = new List<CustomUIDisplay>(orig.displays.Select(x => x.Copy()));
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            Bin = jn["b"].AsInt;
            Layer = jn["l"].AsInt;
            locked = jn["lk"].AsBool;
            collapse = jn["co"].AsBool;

            var tc = jn["tc"];
            if (tc != null)
                textColor = GetDefaultColorVG(tc["r"].AsBool, tc["g"].AsBool, tc["b"].AsBool);
            var bgc = jn["bgc"];
            if (bgc != null)
                color = GetDefaultColorVG(bgc["r"].AsBool, bgc["g"].AsBool, bgc["b"].AsBool);
        }

        public override void ReadJSON(JSONNode jn)
        {
            Bin = jn["bin"].AsInt;
            Layer = jn["layer"].AsInt;
            collapse = jn["shrink"] == null ? jn["collapse"].AsBool : jn["shrink"].AsBool;
            locked = jn["locked"].AsBool;
            selectable = jn["select"] == null || jn["select"].AsBool;
            hidden = jn["hide"].AsBool;
            color = jn["col"];
            selectedColor = jn["selcol"];
            textColor = jn["texcol"];
            markColor = jn["markcol"];
            if (jn["ui"] != null)
                for (int i = 0; i < jn["ui"].Count; i++)
                    displays.Add(CustomUIDisplay.Parse(jn["ui"][i]));
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

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

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

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

            for (int i = 0; i < displays.Count; i++)
                jn["ui"] = displays[i].ToJSON();

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

        /// <summary>
        /// Tries to find the custom UI display with the matching path.
        /// </summary>
        /// <param name="path">Path of the display to find.</param>
        /// <param name="display">Display result.</param>
        /// <returns>Returns true if a display was found, otherwise returns false.</returns>
        public bool TryGetDisplay(string path, out CustomUIDisplay display) => displays.TryFind(x => x.path == path, out display);

        /// <summary>
        /// Gets a custom UI display.
        /// </summary>
        /// <param name="path">Path of the display to find.</param>
        /// <param name="defaultDisplay">Default display to return if no custom display was found.</param>
        /// <returns>Returns the found custom UI display.</returns>
        public CustomUIDisplay GetDisplay(string path, CustomUIDisplay defaultDisplay) => TryGetDisplay(path, out CustomUIDisplay display) ? display : defaultDisplay;

        #endregion
    }
}
