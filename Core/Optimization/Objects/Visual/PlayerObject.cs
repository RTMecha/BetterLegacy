using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for special player objects.
    /// </summary>
    public class PlayerObject : VisualObject
    {
        public override GameObject GameObject { get; set; }
        public override Renderer Renderer { get; set; }
        public override Collider2D Collider { get; set; }

        public PlayerObject(GameObject gameObject) => GameObject = gameObject;

        public override void SetColor(Color color) { }

        public override Color GetPrimaryColor() => Color.white;

        public override void Clear()
        {
            GameObject = null;
            Renderer = null;
            Collider = null;
        }
    }
}
