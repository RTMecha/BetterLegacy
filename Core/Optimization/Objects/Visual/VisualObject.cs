using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Base Class for all VisualObjects.
    /// </summary>
    public abstract class VisualObject
    {
        public abstract GameObject GameObject { get; set; }

        public abstract Renderer Renderer { get; set; }

        public abstract Collider2D Collider { get; set; }

        public abstract void SetColor(Color color);

        public void SetOrigin(Vector3 origin)
        {
            if (!GameObject)
                return;

            GameObject.transform.localPosition = origin;
        }

        public abstract void Clear();
    }
}
