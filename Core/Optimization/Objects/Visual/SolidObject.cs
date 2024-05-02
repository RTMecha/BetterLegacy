using BetterLegacy.Core.Managers;
using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for regular shape objects.
    /// </summary>
    public class SolidObject : VisualObject
    {
        public override GameObject GameObject { get; set; }
        public override Transform Top { get; set; }
        public override Renderer Renderer { get; set; }
        public override Collider2D Collider { get; set; }

        bool opacityCollision;
        public Material material;
        readonly float opacity;

        public SolidObject(GameObject gameObject, Transform top, float opacity, bool hasCollider, bool solid, bool background, bool opacityCollision)
        {
            GameObject = gameObject;
            Top = top;

            this.opacity = opacity;

            Renderer = gameObject.GetComponent<Renderer>();
            Renderer.enabled = true;
            if (background)
            {
                GameObject.layer = 9;
                //Renderer.material = GameStorageManager.inst.bgMaterial;
            }
            Renderer.material = ObjectManager.inst.norm;
            material = Renderer.material;

            Collider = gameObject.GetComponent<Collider2D>();

            if (Collider != null)
            {
                Collider.enabled = true;
                if (hasCollider)
                    Collider.tag = "Helper";

                Collider.isTrigger = !solid;
            }

            this.opacityCollision = opacityCollision;
        }

        public override void SetColor(Color color)
        {
            float a = color.a * opacity;
            material?.SetColor(new Color(color.r, color.g, color.b, a));
            if (opacityCollision)
                Collider.enabled = a > 0.99f;
        }
    }
}
