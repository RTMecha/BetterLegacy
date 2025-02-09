using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents a custom polygon shape used for objects.
    /// </summary>
    public class PolygonShape
    {
        public PolygonShape() { }

        public PolygonShape(int sides, float roundness, float thickness, int slices)
        {
            this.sides = sides;
            this.roundness = roundness;
            this.thickness = thickness;
            this.slices = slices;
        }

        #region Properties

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
            set => slices = RTMath.Clamp(slices, 1, Sides);
        }

        #endregion

        #region Fields

        int sides;
        float roundness;
        float thickness;
        int slices;

        #endregion

        #region Methods

        /// <summary>
        /// Parses a polygon shape from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed polygon shape.</returns>
        public static PolygonShape Parse(JSONNode jn) => new PolygonShape(jn[0].AsInt, jn[1].AsFloat, jn[2].AsFloat, jn[3].AsInt);

        /// <summary>
        /// Writes the <see cref="PolygonShape"/> to a JSON.
        /// </summary>
        /// <returns>Returns a JSON object representing the <see cref="PolygonShape"/>.</returns>
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn[0] = sides;
            jn[1] = roundness;
            jn[2] = thickness;
            jn[3] = slices;
            return jn;
        }

        #endregion
    }
}
