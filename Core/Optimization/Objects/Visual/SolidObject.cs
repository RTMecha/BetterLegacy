﻿using UnityEngine;

using LSFunctions;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for regular shape objects.
    /// </summary>
    public class SolidObject : VisualObject
    {
        /// <summary>
        /// Material of the solid object.
        /// </summary>
        public Material material;

        readonly bool opacityCollision;
        readonly float opacity;

        int gradientType;

        /// <summary>
        /// If the gradient is linear.
        /// </summary>
        public bool IsLinear => gradientType <= 2;

        /// <summary>
        /// If the gradient is flipped.
        /// </summary>
        public bool IsFlipped => gradientType == 1 || gradientType == 3;

        public SolidObject(GameObject gameObject, float opacity, bool hasCollider, bool solid, bool background, bool opacityCollision, int gradientType, float gradientScale, float gradientRotation)
        {
            this.gameObject = gameObject;

            this.opacity = opacity;

            renderer = gameObject.GetComponent<Renderer>();
            renderer.enabled = true;

            UpdateRendering(gradientType, background, false, gradientScale, gradientRotation);

            collider = gameObject.GetComponent<Collider2D>();

            if (collider)
            {
                collider.enabled = true;
                if (hasCollider)
                    collider.tag = Tags.HELPER;

                collider.isTrigger = !solid;
            }

            this.opacityCollision = opacityCollision;
        }

        /// <summary>
        /// Updates the objects' materials based on specific values.
        /// </summary>
        /// <param name="gradientType">Type of gradient to render.</param>
        public void UpdateRendering(int gradientType, bool background, bool doubleSided = false, float gradientScale = 1f, float gradientRotation = 0f)
        {
            gameObject.layer = background ? 9 : 8;

            isGradient = gradientType != 0;
            this.gradientType = gradientType;

            if (isGradient)
                renderer.material = IsLinear ?
                    (doubleSided ? LegacyResources.gradientDoubleSidedMaterial : LegacyResources.gradientMaterial) :
                    (doubleSided ? LegacyResources.radialGradientDoubleSidedMaterial : LegacyResources.radialGradientMaterial);

            else
                renderer.material = doubleSided ? LegacyResources.objectDoubleSidedMaterial : LegacyResources.objectMaterial;

            material = renderer.material;

            if (isGradient)
                TranslateGradient(gradientScale, gradientRotation);
        }

        public override void InterpolateColor(float time)
        {
            if (isGradient)
            {
                SetColor(colorSequence.Interpolate(time), secondaryColorSequence.Interpolate(time));
                return;
            }

            base.InterpolateColor(time);
        }

        public override void SetColor(Color color)
        {
            float a = color.a * opacity;
            material?.SetColor(new Color(color.r, color.g, color.b, a));
            if (opacityCollision)
                colliderEnabled = a > 0.99f;
        }

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
                colliderEnabled = color.a + color2.a > 1.99f;
        }

        public override Color GetPrimaryColor() => material.color;

        /// <summary>
        /// Gets the gradient objects' secondary color.
        /// </summary>
        /// <returns>Returns the secondary color of the gradient object.</returns>
        public override Color GetSecondaryColor() => !isGradient ? LSColors.pink500 : material.GetColor("_ColorSecondary");

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

        /// <summary>
        /// Gets the colors of the gradient.
        /// </summary>
        /// <returns>Returns the gradient colors.</returns>
        public GradientColors GetColors() => new GradientColors(GetColor(true), GetColor(false));

        /// <summary>
        /// Changes the scale and rotation of the gradient.
        /// </summary>
        /// <param name="scale">Scale of the gradient.</param>
        /// <param name="rotation">Rotation of the gradient.</param>
        public void TranslateGradient(float scale = 1f, float rotation = 0f)
        {
            if (!isGradient)
                return;

            material.SetFloat("_Scale", scale);
            if (IsLinear)
                material.SetFloat("_Rotation", rotation);
        }

        public override void Clear()
        {
            base.Clear();
            material = null;
        }
    }
}
