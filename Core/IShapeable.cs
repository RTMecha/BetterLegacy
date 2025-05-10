using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core
{
    /// <summary>
    /// Indicates an object can apply shape groups.
    /// </summary>
    public interface IShapeable
    {
        /// <summary>
        /// Shape group.
        /// </summary>
        public int Shape { get; set; }

        /// <summary>
        /// Shape option.
        /// </summary>
        public int ShapeOption { get; set; }

        /// <summary>
        /// Text data for <see cref="ShapeType.Text"/> or image path data for <seealso cref="ShapeType.Image"/>.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// If the <see cref="ShapeType.Text"/> object should align the text to the origin.
        /// </summary>
        public bool AutoTextAlign { get; set; }

        /// <summary>
        /// Settings for the custom polygon shape.
        /// </summary>
        public Data.Beatmap.PolygonShape Polygon { get; set; }

        /// <summary>
        /// Type of the shape.
        /// </summary>
        public ShapeType ShapeType { get; set; }

        /// <summary>
        /// If the shape has special properties: Text or Image.
        /// </summary>
        public bool IsSpecialShape { get; }

        /// <summary>
        /// Used for modifying the shape objects' shape at runtime via modifiers.
        /// </summary>
        /// <param name="shape">Shape group.</param>
        /// <param name="shapeOption">Shape option.</param>
        public void SetCustomShape(int shape, int shapeOption);
    }
}
