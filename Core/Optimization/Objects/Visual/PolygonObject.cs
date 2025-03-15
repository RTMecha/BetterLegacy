using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for polygon shape objects.
    /// </summary>
    public class PolygonObject : VisualObject
    {
        public Material material;

        readonly bool opacityCollision;
        readonly float opacity;

        public PolygonObject(GameObject gameObject, float opacity, bool hasCollider, bool solid, bool background, bool opacityCollision, PolygonShape polygonShape)
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
