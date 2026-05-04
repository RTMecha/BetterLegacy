using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents settings for drawing annotations.
    /// </summary>
    public class AnnotationSettings : PAObject<AnnotationSettings>
    {
        #region Values

        /// <summary>
        /// Tool to use for annotation.
        /// </summary>
        public AnnotationTool tool;

        /// <summary>
        /// Color to draw.
        /// </summary>
        public int color;

        /// <summary>
        /// Custom hex color to draw.
        /// </summary>
        public string hexColor;

        /// <summary>
        /// Opacity to draw.
        /// </summary>
        public float opacity = 1f;

        /// <summary>
        /// Thickness of the lines to draw.
        /// </summary>
        public float thickness = 4f;

        /// <summary>
        /// If the drawn annotations should be fixed to the camera.
        /// </summary>
        public bool fixedCamera;

        /// <summary>
        /// If drawing should be mirrored horizontally.
        /// </summary>
        public bool mirrorDrawingHorizontal;

        /// <summary>
        /// If drawing should be mirrored vertically.
        /// </summary>
        public bool mirrorDrawingVertical;

        #endregion

        #region Functions

        public override void CopyData(AnnotationSettings orig, bool newID = true)
        {
            tool = orig.tool;
            color = orig.color;
            hexColor = orig.hexColor;
            opacity = orig.opacity;
            thickness = orig.thickness;
            fixedCamera = orig.fixedCamera;
            mirrorDrawingHorizontal = orig.mirrorDrawingHorizontal;
            mirrorDrawingVertical = orig.mirrorDrawingVertical;
        }

        public override void ReadJSON(JSONNode jn)
        {
            tool = Parser.TryParse(jn["tool"], AnnotationTool.None);
            color = jn["col"].AsInt;
            hexColor = jn["hex_col"];
            if (jn["opacity"] != null)
                opacity = jn["opacity"].AsFloat;
            if (jn["thickness"] != null)
                thickness = jn["thickness"].AsFloat;
            fixedCamera = jn["fixed_camera"].AsBool;
            if (jn["mirror"] != null)
            {
                mirrorDrawingHorizontal = jn["mirror"]["horizontal"].AsBool;
                mirrorDrawingVertical = jn["mirror"]["vertical"].AsBool;
            }
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["tool"] = tool.ToString();
            jn["col"] = color;
            if (!string.IsNullOrEmpty(hexColor))
                jn["hex_col"] = hexColor;
            if (thickness != 4f)
                jn["thickness"] = thickness;
            if (fixedCamera)
                jn["fixed_camera"] = fixedCamera;
            if (mirrorDrawingHorizontal)
                jn["mirror"]["horizontal"] = mirrorDrawingHorizontal;
            if (mirrorDrawingVertical)
                jn["mirror"]["vertical"] = mirrorDrawingVertical;

            return jn;
        }

        #endregion
    }
}
