using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Base Class for all VisualObjects.
    /// </summary>
    public abstract class VisualObject
    {
        public abstract GameObject GameObject { get; set; }

        public abstract Transform Top { get; set; }

        public abstract Renderer Renderer { get; set; }

        public abstract Collider2D Collider { get; set; }

        public abstract void SetColor(Color color);
    }
}
