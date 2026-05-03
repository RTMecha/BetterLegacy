using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents an annotation that displays in the editor. Useful for storyboarding and visual notes.
    /// </summary>
    public class Annotation : PAObject<Annotation>
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
        /// Thickness of the annotation.
        /// </summary>
        public float thickness = 4f;

        /// <summary>
        /// Points the annotation should render.
        /// </summary>
        public List<Vector2> points = new List<Vector2>();

        #endregion

        #endregion

        #region Functions

        public override void CopyData(Annotation orig, bool newID = true)
        {
            id = orig.id;

            color = orig.color;
            opacity = orig.opacity;
            hexColor = orig.hexColor;

            thickness = orig.thickness;

            points = new List<Vector2>(orig.points);
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

            return jn;
        }

        #endregion
    }
}
