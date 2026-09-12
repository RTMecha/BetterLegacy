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

        /// <summary>
        /// Active state of the object.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Position of the object.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Scale of the object.
        /// </summary>
        public Vector2 Scale { get; set; }

        /// <summary>
        /// Rotation of the object.
        /// </summary>
        public float Rotation { get; set; }

        /// <summary>
        /// Color slot of the object.
        /// </summary>
        public int Color { get; set; }

        /// <summary>
        /// Custom hex color of the object.
        /// </summary>
        public string CustomColor { get; set; }

        /// <summary>
        /// Opacity of the object.
        /// </summary>
        public float Opacity { get; set; }

        /// <summary>
        /// Depth of the object.
        /// </summary>
        public float Depth { get; set; }

        #endregion

        #region Extra

        /// <summary>
        /// Handles <see cref="TrailRenderer"/> properties.
        /// </summary>
        public PlayerTrail Trail { get; set; }

        /// <summary>
        /// Handles <see cref="ParticleSystem"/> properties.
        /// </summary>
        public PlayerParticles Particles { get; set; }

        #endregion
    }
}
