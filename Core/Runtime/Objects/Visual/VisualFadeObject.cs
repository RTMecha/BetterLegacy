using UnityEngine;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Runtime.Objects.Visual
{
    /// <summary>
    /// Represents a single fade object for Background Objects.
    /// </summary>
    public class VisualFadeObject : Exists
    {
        public VisualFadeObject() { }

        public VisualFadeObject(GameObject gameObject, Renderer renderer, MeshFilter meshFilter)
        {
            this.gameObject = gameObject;
            this.renderer = renderer;
            this.meshFilter = meshFilter;
        }

        /// <summary>
        /// The visual objects' game object.
        /// </summary>
        public GameObject gameObject;
        /// <summary>
        /// The visual objects' renderer.
        /// </summary>
        public Renderer renderer;
        /// <summary>
        /// The visual objects' mesh filter.
        /// </summary>
        public MeshFilter meshFilter;
        public bool Active { get; set; } = true;

        /// <summary>
        /// Sets the active state of the visual object.
        /// </summary>
        /// <param name="state">Active state to set.</param>
        public void SetActive(bool state)
        {
            Active = state;
            if (gameObject)
                gameObject.SetActive(state);
        }

        public void SetColor(Color color)
        {
            if (renderer)
                renderer.material.color = color;
        }
    }
}
