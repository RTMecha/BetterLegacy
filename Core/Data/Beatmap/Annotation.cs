using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Editor.Data;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents an annotation that displays in the editor. Useful for storyboarding and visual notes.
    /// </summary>
    public class Annotation : PAObject<Annotation>, IPacket, ISelectable
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

        public void ReadPacket(NetworkReader reader)
        {
            color = reader.ReadInt32();
            opacity = reader.ReadSingle();
            hexColor = reader.ReadString();

            thickness = reader.ReadSingle();
            points = reader.ReadList(reader.ReadVector2);
            fixedCamera = reader.ReadBoolean();
            hidden = reader.ReadBoolean();
            selected = reader.ReadBoolean();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(color);
            writer.Write(opacity);
            writer.Write(hexColor);

            writer.Write(thickness);
            writer.Write(points, writer.Write);
            writer.Write(fixedCamera);
            writer.Write(hidden);
            writer.Write(selected);
        }

        /// <summary>
        /// Draws a box shape.
        /// </summary>
        /// <param name="pos">Position of the box.</param>
        /// <param name="sca">Scale of the box.</param>
        /// <param name="rot">Rotation of the box.</param>
        public void DrawBox(Vector2 pos, Vector2 sca, float rot)
        {
            points.Clear();
            var topLeftCorner = new Vector2(pos.x - (sca.x / 2f), pos.y + (sca.y / 2f));
            var topRightCorner = new Vector2(pos.x + (sca.x / 2f), pos.y + (sca.y / 2f));
            var bottomRightCorner = new Vector2(pos.x + (sca.x / 2f), pos.y - (sca.y / 2f));
            var bottomLeftCorner = new Vector2(pos.x - (sca.x / 2f), pos.y - (sca.y / 2f));

            for (int i = 0; i < (int)(sca.x * 2); i++)
                points.Add(RTMath.Lerp(topLeftCorner, topRightCorner, i / (sca.x * 2)));
            for (int i = 0; i < (int)(sca.y * 2); i++)
                points.Add(RTMath.Lerp(topRightCorner, bottomRightCorner, i / (sca.y * 2)));
            for (int i = 0; i < (int)(sca.x * 2); i++)
                points.Add(RTMath.Lerp(bottomRightCorner, bottomLeftCorner, i / (sca.x * 2)));
            for (int i = 0; i < (int)(sca.y * 2) + 1; i++)
                points.Add(RTMath.Lerp(bottomLeftCorner, topLeftCorner, i / (sca.y * 2)));
            points = points.Select(x => (Vector2)RTMath.Rotate(x, rot)).ToList();
        }

        /// <summary>
        /// Draws a polygon shape.
        /// </summary>
        /// <param name="count">Side count.</param>
        /// <param name="pos">Position of the shape.</param>
        /// <param name="sca">Scale of the shape.</param>
        /// <param name="rot">Rotation of the shape.</param>
        public void DrawShape(int count, Vector2 pos, Vector2 sca, float rot)
        {
            points.Clear();
            for (int i = 0; i < count + 1; i++)
            {
                var r = 360f * (i / (float)count);
                var x = Mathf.Sin(r / Mathf.Rad2Deg);
                var y = Mathf.Cos(r / Mathf.Rad2Deg);
                points.Add(new Vector2((x * sca.x) + pos.x, (y * sca.y) + pos.y));
            }
            points = points.Select(x => (Vector2)RTMath.Rotate(x, rot)).ToList();
        }

        /// <summary>
        /// Draws a line.
        /// </summary>
        /// <param name="startPos">Start position.</param>
        /// <param name="endPos">End position.</param>
        public void DrawLine(Vector2 startPos, Vector2 endPos)
        {
            points.Clear();
            var count = (int)RTMath.Distance(startPos, endPos) * 10;
            for (int i = 0; i < count; i++)
                points.Add(RTMath.Lerp(startPos, endPos, i / (float)count));
        }

        #endregion
    }
}
