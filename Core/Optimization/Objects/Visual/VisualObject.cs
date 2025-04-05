using UnityEngine;

using LSFunctions;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Base Class for all VisualObjects.
    /// </summary>
    public abstract class VisualObject : Exists
    {
        /// <summary>
        /// The visual objects' game object.
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// The visual objects' renderer.
        /// </summary>
        public Renderer renderer;

        /// <summary>
        /// The visual objects' collider.
        /// </summary>
        public Collider2D collider;

        /// <summary>
        /// If the <see cref="collider"/> component is dynamically enabled.
        /// </summary>
        public bool colliderEnabled = true;

        /// <summary>
        /// Color sequence to interpolate through.
        /// </summary>
        public Sequence<Color> colorSequence;

        /// <summary>
        /// Gradient color sequence to interpolate through.
        /// </summary>
        public Sequence<Color> secondaryColorSequence;

        /// <summary>
        /// If the object renders as a gradient.
        /// </summary>
        public bool isGradient;

        /// <summary>
        /// Sets the render type layer of the object.
        /// </summary>
        /// <param name="renderType">Render type to set.</param>
        public void SetRenderType(int renderType) => gameObject.layer = renderType switch
        {
            1 => 9,
            2 => 11,
            _ => 8
        };

        /// <summary>
        /// Interpolates the visual objects' colors.
        /// </summary>
        /// <param name="time">Time scale.</param>
        public virtual void InterpolateColor(float time) => SetColor(colorSequence.Interpolate(time));

        /// <summary>
        /// Sets the visual objects' main color.
        /// </summary>
        /// <param name="color">Color to set.</param>
        public abstract void SetColor(Color color);

        /// <summary>
        /// Gets the visual objects' main color.
        /// </summary>
        /// <returns>Returns the primary color of the visual object.</returns>
        public abstract Color GetPrimaryColor();

        /// <summary>
        /// Gets the gradient objects' secondary color.
        /// </summary>
        /// <returns>Returns the secondary color of the gradient object.</returns>
        public virtual Color GetSecondaryColor() => LSColors.pink500;

        /// <summary>
        /// Sets the origin of the visual object.
        /// </summary>
        /// <param name="origin">Origin to set.</param>
        public virtual void SetOrigin(Vector3 origin)
        {
            if (!gameObject)
                return;

            gameObject.transform.localPosition = origin;
        }

        /// <summary>
        /// Sets the scale offset of the visual object.
        /// </summary>
        /// <param name="scale">Scale to set.</param>
        public void SetScaleOffset(Vector2 scale)
        {
            if (gameObject)
                gameObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        /// <summary>
        /// Sets the rotation offset of the visual object.
        /// </summary>
        /// <param name="rot">Rotation to set.</param>
        public void SetRotationOffset(float rot)
        {
            if (gameObject)
                gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        /// <summary>
        /// Clears the visual object data.
        /// </summary>
        public virtual void Clear()
        {
            gameObject = null;
            renderer = null;
            collider = null;
        }
    }
}
