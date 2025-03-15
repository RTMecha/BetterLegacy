using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for regular shape objects.
    /// </summary>
    public class SolidObject : VisualObject
    {
        public Material material;

        readonly bool opacityCollision;
        readonly float opacity;

        public SolidObject(GameObject gameObject, float opacity, bool hasCollider, bool solid, bool background, bool opacityCollision)
        {
            this.gameObject = gameObject;

            this.opacity = opacity;

            renderer = gameObject.GetComponent<Renderer>();
            renderer.enabled = true;
            if (background)
            {
                this.gameObject.layer = 9;
                renderer.material = ObjectManager.inst.norm; // todo: replace with a material that supports perspective and doesn't have issues with opacity
            }

            material = renderer.material;

            collider = gameObject.GetComponent<Collider2D>();

            if (collider != null)
            {
                collider.enabled = true;
                if (hasCollider)
                    collider.tag = Tags.HELPER;

                collider.isTrigger = !solid;
            }

            this.opacityCollision = opacityCollision;
        }

        public override void SetColor(Color color)
        {
            float a = color.a * opacity;
            material?.SetColor(new Color(color.r, color.g, color.b, a));
            if (opacityCollision)
                colliderEnabled = a > 0.99f;
        }

        public override Color GetPrimaryColor() => material.color;

        public override void Clear()
        {
            base.Clear();
            material = null;
        }
    }
}
