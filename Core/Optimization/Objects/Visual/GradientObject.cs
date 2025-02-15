﻿using System;
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

        public bool IsFlipped => gradientType == 1 || gradientType == 3;
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
                    Collider.tag = Tags.HELPER;

                Collider.isTrigger = !solid;
            }

            this.opacityCollision = opacityCollision;
        }

        public override void SetColor(Color color) => material.color = new Color(color.r, color.g, color.b, color.a * opacity);

        /// <summary>
        /// Sets the gradient objects' colors.
        /// </summary>
        /// <param name="color">Primary color to set.</param>
        /// <param name="color2">Secondary color to set.</param>
        public void SetColor(Color color, Color color2)
        {
            if (color2.a < 0) //no custom opacity, it means it's an alpha gradient
            {
                color2.a = color.a;

                if (color.r == color2.r && color.g == color2.g && color.b == color2.b)
                    color2.a = 0;
            }

            if (IsFlipped)
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
                ColliderEnabled = color.a + color2.a > 1.99f;
        }

        public override Color GetPrimaryColor() => material.color;

        /// <summary>
        /// Gets the gradient objects' secondary color.
        /// </summary>
        /// <returns>Returns the secondary color of the gradient object.</returns>
        public Color GetSecondaryColor() => material.GetColor("_ColorSecondary");

        /// <summary>
        /// Gets a specified color based on the gradients' flipped state.
        /// </summary>
        /// <param name="primary">If the color should be primary.</param>
        /// <returns>Returns a gradients color.</returns>
        public Color GetColor(bool primary)
        {
            if (primary)
                return IsFlipped ? GetSecondaryColor() : GetPrimaryColor();

            return IsFlipped ? GetPrimaryColor() : GetSecondaryColor();
        }

        public GradientColors GetColors() => new GradientColors(GetColor(true), GetColor(false));

        public override void Clear()
        {
            GameObject = null;
            Renderer = null;
            Collider = null;
            material = null;
        }
    }

    public struct GradientColors
    {
        public GradientColors(Color startColor, Color endColor)
        {
            this.startColor = startColor;
            this.endColor = endColor;
        }

        public Color startColor;
        public Color endColor;
    }
}
