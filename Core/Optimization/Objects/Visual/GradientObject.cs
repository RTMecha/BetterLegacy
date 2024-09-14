using System;
using BetterLegacy.Patchers;
using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for gradient objects.
    /// </summary>
    public class GradientObject : VisualObject
    {
        public override GameObject GameObject { get; set; }
        public override Renderer Renderer { get; set; }
        public override Collider2D Collider { get; set; }
        public Material material;

        readonly bool opacityCollision;
        readonly float opacity;
        readonly int gradientType;
        public GradientObject(GameObject gameObject, float opacity, bool hasCollider, bool solid, bool background, bool opacityCollision, int gradientType)
        {
            GameObject = gameObject;

            this.opacity = opacity;
            this.gradientType = gradientType;
            
            Renderer = gameObject.GetComponent<Renderer>();
            Renderer.enabled = true;
            if (background)
                GameObject.layer = 9;

            Renderer.material = gradientType <= 2 ? LegacyPlugin.gradientMaterial : LegacyPlugin.radialGradientMaterial;
           
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
            throw new NotImplementedException();
        }

        public void SetColor(Color color, Color color2)
        {
            if (color2.a < 0) //no custom opacity, it means it's an alpha gradient
            {
                color2.a = color.a;

                if (color.r == color2.r && color.g == color2.g && color.b == color2.b)
                    color2.a = 0;
            }

            if (gradientType == 1 || gradientType == 3)
            {
                material.SetColor("_Color", new Color(color2.r, color2.g, color2.b, color2.a * opacity));
                material.SetColor("_ColorSecondary", new Color(color.r, color.g, color.b, color.a * opacity));
            }
            else
            {
                material.SetColor("_Color", new Color(color.r, color.g, color.b, color.a * opacity));
                material.SetColor("_ColorSecondary", new Color(color2.r, color2.g, color2.b, color2.a * opacity));
            }
            
            if (opacityCollision)
                Collider.enabled = color.a + color2.a > 1.99f;
        }
        
        public override void Clear()
        {
            GameObject = null;
            Renderer = null;
            Collider = null;
            material = null;
        }
    }
}
