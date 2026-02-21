using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents standalone shape data.
    /// </summary>
    public class ShapeableData : Exists
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
        public PolygonShape Polygon { get; set; }

        /// <summary>
        /// Type of the shape.
        /// </summary>
        public ShapeType ShapeType { get => (ShapeType)Shape; set => Shape = (int)value; }

        /// <summary>
        /// If the shape has special properties: Text or Image.
        /// </summary>
        public bool IsSpecialShape => ShapeType == ShapeType.Text || ShapeType == ShapeType.Image;

        /// <summary>
        /// Converts an <see cref="IShapeable"/> object to <see cref="ShapeableData"/>.
        /// </summary>
        /// <param name="shapeable">Shapeable object reference.</param>
        /// <returns>Returns a new <see cref="ShapeableData"/>.</returns>
        public static ShapeableData FromIShapeable(IShapeable shapeable) => new ShapeableData
        {
            Shape = shapeable.Shape,
            ShapeOption = shapeable.ShapeOption,
            Text = shapeable.Text,
            AutoTextAlign = shapeable.AutoTextAlign,
            Polygon = shapeable.Polygon,
        };

        /// <summary>
        /// Applies shape data to an <see cref="IShapeable"/> object.
        /// </summary>
        /// <param name="shapeable">Shapeable object reference.</param>
        public void ApplyShapeData(IShapeable shapeable)
        {
            shapeable.Shape = Shape;
            shapeable.ShapeOption = ShapeOption;
            shapeable.Text = Text;
            shapeable.AutoTextAlign = AutoTextAlign;
            shapeable.Polygon = Polygon;
        }
    }
}
