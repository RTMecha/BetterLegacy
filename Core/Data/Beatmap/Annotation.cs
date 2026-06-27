using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Editor.Data;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents an annotation that displays in the editor. Useful for storyboarding and visual notes.
    /// </summary>
    public class Annotation : PAObject<Annotation>, ISelectable
    {
        #region Constructors

        public Annotation() { }

        #endregion

        #region Values

        #region Color

        /// <summary>
        /// Color of the annotation.
        /// </summary>
        public int color;

        /// <summary>
        /// Opacity of the annotation.
        /// </summary>
        public float opacity = 1f;

        /// <summary>
        /// Hex color of the annotation.
        /// </summary>
        public string hexColor;

        #endregion

        #region Display

        /// <summary>
        /// If the annotation should be hidden.
        /// </summary>
        public bool hidden;

        /// <summary>
        /// Thickness of the annotation.
        /// </summary>
        public float thickness = 4f;

        /// <summary>
        /// Points the annotation should render.
        /// </summary>
        public List<Vector2> points = new List<Vector2>();

        /// <summary>
        /// If the annotation is fixed to the camera.
        /// </summary>
        public bool fixedCamera;

        #endregion

        /// <summary>
        /// If the annotation is selected.
        /// </summary>
        public bool selected;

        public bool Selected { get => selected; set => selected = value; }

        /// <summary>
        /// Amount to multiply / divide the thickness when reading from / writing to a VG format file.
        /// </summary>
        public static float thicknessVGMultiply = 0.1f;

        #endregion

        #region Functions

        public override void CopyData(Annotation orig, bool newID = true)
        {
            id = orig.id;

            color = orig.color;
            opacity = orig.opacity;
            hexColor = orig.hexColor;

            thickness = orig.thickness;
            hidden = orig.hidden;
            points = new List<Vector2>(orig.points);
            fixedCamera = orig.fixedCamera;
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            id = jn["id"];
            color = jn["c"].AsInt;
            thickness = jn["t"].AsFloat * thicknessVGMultiply;
            for (int i = 0; i < jn["p"].Count; i++)
                points.Add(Parser.TryParse(jn["p"][i], Vector2.zero));
        }

        public override void ReadJSON(JSONNode jn)
        {
            color = jn["c"].AsInt;
            if (jn["o"] != null)
                opacity = jn["o"].AsFloat;
            hexColor = jn["hc"];

            if (jn["th"] != null)
                thickness = jn["th"].AsFloat;
            if (jn["p"] != null)
            {
                points.Clear();
                for (int i = 0; i < jn["p"].Count; i++)
                    points.Add(jn["p"][i].AsVector2());
            }
            fixedCamera = jn["f"].AsBool;
            hidden = jn["h"].AsBool;
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id ?? GetStringID();
            jn["c"] = color;
            jn["t"] = thickness / thicknessVGMultiply;
            for (int i = 0; i < points.Count; i++)
                jn["p"][i] = points[i].ToJSON();

            return jn;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (color != 0)
                jn["c"] = color;
            if (opacity != 1f)
                jn["o"] = opacity;
            if (!string.IsNullOrEmpty(hexColor))
                jn["hc"] = hexColor;

            if (thickness != 4f)
                jn["th"] = thickness;
            for (int i = 0; i < points.Count; i++)
                jn["p"][i] = points[i].ToJSONArray();
            if (fixedCamera)
                jn["f"] = fixedCamera;
            if (hidden)
                jn["h"] = hidden;

            return jn;
        }

        #endregion
    }
}
