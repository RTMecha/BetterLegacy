using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Indicates an object is an object used for player models.
    /// </summary>
    public interface IPlayerObject
    {
        #region Shape

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
        public ShapeType ShapeType { get; set; }

        /// <summary>
        /// If the shape has special properties: Text or Image.
        /// </summary>
        public bool IsSpecialShape { get; }

        #endregion

        #region Main

        public bool Active { get; set; }

        public Vector2 Position { get; set; }

        public Vector2 Scale { get; set; }

        public float Rotation { get; set; }

        public int Color { get; set; }

        public string CustomColor { get; set; }

        public float Opacity { get; set; }

        public float Depth { get; set; }

        #endregion

        #region Extra

        public PlayerTrail Trail { get; set; }
        public PlayerParticles Particles { get; set; }

        #endregion
    }

}
