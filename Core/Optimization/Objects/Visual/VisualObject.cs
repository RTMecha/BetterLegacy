using BetterLegacy.Core.Data;
using UnityEngine;

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
        public abstract GameObject GameObject { get; set; }

        /// <summary>
        /// The visual objects' renderer.
        /// </summary>
        public abstract Renderer Renderer { get; set; }

        /// <summary>
        /// The visual objects' collider.
        /// </summary>
        public abstract Collider2D Collider { get; set; }

        public virtual bool ColliderEnabled { get; set; } = true;

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
        /// Sets the origin of the visual object.
        /// </summary>
        /// <param name="origin">Origin to set.</param>
        public virtual void SetOrigin(Vector3 origin)
        {
            if (!GameObject)
                return;

            GameObject.transform.localPosition = origin;
        }

        /// <summary>
        /// Sets the scale offset of the visual object.
        /// </summary>
        /// <param name="scale">Scale to set.</param>
        public void SetScaleOffset(Vector2 scale)
        {
            if (GameObject)
                GameObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        /// <summary>
        /// Sets the rotation offset of the visual object.
        /// </summary>
        /// <param name="rot">Rotation to set.</param>
        public void SetRotationOffset(float rot)
        {
            if (GameObject)
                GameObject.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        /// <summary>
        /// Clears the visual object data.
        /// </summary>
        public abstract void Clear();
    }
}
