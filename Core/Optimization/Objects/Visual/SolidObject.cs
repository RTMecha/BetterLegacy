using BetterLegacy.Core.Data;
using LSFunctions;
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

        int gradientType;
        public bool IsFlipped => gradientType == 1 || gradientType == 3;

        public SolidObject(GameObject gameObject, float opacity, bool hasCollider, bool solid, bool background, bool opacityCollision, int gradientType)
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

            UpdateMaterial(gradientType);
            material = renderer.material;

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

        public void UpdateMaterial(int gradientType)
        {
            isGradient = gradientType != 0;
            this.gradientType = gradientType;

            if (isGradient)
                renderer.material = gradientType <= 2 ? LegacyPlugin.gradientMaterial : LegacyPlugin.radialGradientMaterial;

            material = renderer.material;
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

        public override void Clear()
        {
            base.Clear();
            material = null;
        }
    }
}
