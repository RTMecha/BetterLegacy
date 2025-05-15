using UnityEngine;

using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents a custom polygon shape used for objects.
    /// </summary>
    public class PolygonShape : PAObject<PolygonShape>
    {
        public PolygonShape() { }

        public PolygonShape(int sides, float roundness, float thickness, int slices, Vector2 thicknessOffset = default, Vector2 thicknessScale = default)
        {
            this.sides = sides;
            this.roundness = roundness;
            this.thickness = thickness;
            this.slices = slices;
            this.thicknessOffset = thicknessOffset;
            this.thicknessScale = thicknessScale;
        }

        #region Properties

        /// <summary>
        /// Radius of the shape.
        /// </summary>
        public float Radius
        {
            get => Mathf.Clamp(radius, 0.1f, 10f);
            set => radius = Mathf.Clamp(value, 0.1f, 10f);
        }

        /// <summary>
        /// How many sides the polygon shape has.
        /// </summary>
        public int Sides
        {
            get => RTMath.Clamp(sides, 3, 32);
            set => sides = RTMath.Clamp(value, 3, 32);
        }

        /// <summary>
        /// Roundness of the polygon shapes' corners.
        /// </summary>
        public float Roundness
        {
            get => RTMath.Clamp(roundness, 0f, 1f);
            set => roundness = RTMath.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Outline thickness of the polygon shape.
        /// </summary>
        public float Thickness
        {
            get => RTMath.Clamp(thickness, 0f, 1f);
            set => thickness = RTMath.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// How many slices of the polygon shape to display.
        /// </summary>
        public int Slices
        {
            get => RTMath.Clamp(slices, 1, Sides);
            set => slices = RTMath.Clamp(value, 1, Sides);
        }

        /// <summary>
        /// Outline thickness offset of the polygon shape.
        /// </summary>
        public Vector2 ThicknessOffset
        {
            get => thicknessOffset;
            set => thicknessOffset = value;
        }

        /// <summary>
        /// Outline thickness scale of the polygon shape.
        /// </summary>
        public Vector2 ThicknessScale
        {
            get => thicknessScale;
            set => thicknessScale = value;
        }

        #endregion

        #region Fields

        float radius = 0.5f;
        int sides = 3;
        float roundness;
        float thickness = 1f;
        int slices = 3;
        Vector2 thicknessOffset;
        Vector2 thicknessScale = Vector2.one;

        #endregion

        #region Methods

        public override void CopyData(PolygonShape orig, bool newID = true)
        {
            Sides = orig.Sides;
            Roundness = orig.Roundness;
            Thickness = orig.Thickness;
            Slices = orig.Slices;
            ThicknessOffset = orig.ThicknessOffset;
            ThicknessScale = orig.ThicknessScale;
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            Sides = jn[0].AsInt;
            Roundness = jn[1].AsFloat;
            Thickness = jn[2].AsFloat;
            Slices = jn[3].AsInt;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn.IsArray)
            {
                ReadJSONVG(jn);
                return;
            }

            if (jn["ra"] != null)
                Radius = jn["ra"].AsFloat;
            Sides = jn["si"].AsInt;
            Roundness = jn["ro"].AsFloat;
            Thickness = jn["th"].AsFloat;
            Slices = jn["sl"].AsInt;
            ThicknessOffset = Parser.TryParse(jn["tho"], Vector2.zero);
            ThicknessScale = Parser.TryParse(jn["ths"], Vector2.one);
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONArray();
            jn[0] = sides;
            jn[1] = roundness;
            jn[2] = thickness;
            jn[3] = slices;
            return jn;
        }

        /// <summary>
        /// Writes the <see cref="PolygonShape"/> to a JSON.
        /// </summary>
        /// <returns>Returns a JSON object representing the <see cref="PolygonShape"/>.</returns>
        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();
            if (radius != 0.5f)
                jn["ra"] = radius;
            jn["si"] = sides;
            jn["ro"] = roundness;
            jn["th"] = thickness;
            jn["sl"] = slices;
            if (thicknessOffset.x != 0f || thicknessOffset.y != 0f)
                jn["tho"] = thicknessOffset.ToJSONArray();
            if (thicknessScale.x != 1f || thicknessScale.y != 1f)
                jn["ths"] = thicknessScale.ToJSONArray();
            return jn;
        }

        #endregion
    }
}
