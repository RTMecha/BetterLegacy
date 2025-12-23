using UnityEngine;

using LSFunctions;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects.Visual
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

        bool opacityCollision;

        /// <summary>
        /// Cached double sided.
        /// </summary>
        public bool doubleSided;

        /// <summary>
        /// Cached gradient type.
        /// </summary>
        public int gradientType;

        /// <summary>
        /// If the gradient is linear.
        /// </summary>
        public bool IsLinear => gradientType <= 2;

        /// <summary>
        /// If the gradient is flipped.
        /// </summary>
        public bool IsFlipped => gradientType == 1 || gradientType == 3;

        /// <summary>
        /// Cached gradient scale.
        /// </summary>
        public float gradientScale;

        /// <summary>
        /// Cached gradient rotation.
        /// </summary>
        public float gradientRotation;

        /// <summary>
        /// Cached color blend mode.
        /// </summary>
        public int colorBlendMode;

        /// <summary>
        /// If the collision can't damage the player.
        /// </summary>
        public bool deco;
        /// <summary>
        /// If the collision is solid.
        /// </summary>
        public bool solid;
        /// <summary>
        /// If the opacity value is high enough to turn collision on.
        /// </summary>
        public bool opacityCollide = true;
        /// <summary>
        /// If the collision should be forced on, ignoring optimization. Specifically used for collision detection trigger modifiers.
        /// </summary>
        public bool forceCollisionEnabled;

        public override bool HasCollision => (forceCollisionEnabled || solid || !RTBeatmap.Current.Invincible && !deco || CoreHelper.IsEditing && EditorConfig.Instance.SelectObjectsInPreview.Value) && colliderEnabled && opacityCollide;

        /// <summary>
        /// If the object is rendering an outline.
        /// </summary>
        public bool hasOutline;
        /// <summary>
        /// Outline data.
        /// </summary>
        public OutlineData outlineData;
        /// <summary>
        /// Type of the outline.
        /// </summary>
        public int outlineType;
        /// <summary>
        /// If the object is rendering an outline for the editor.
        /// </summary>
        public bool hasEditorOutline;
        /// <summary>
        /// Outline data for the editor.
        /// </summary>
        public OutlineData editorOutlineData;

        Color primaryColor;
        Color secondaryColor;

        public SolidObject(GameObject gameObject, float opacity, bool deco, bool solid, int renderType, bool opacityCollision, int gradientType, float gradientScale, float gradientRotation, int colorBlendMode)
        {
            this.gameObject = gameObject;

            this.opacity = opacity;

            renderer = gameObject.GetComponent<Renderer>();
            renderer.enabled = true;

            collider = gameObject.GetComponent<Collider2D>();

            UpdateRendering(gradientType, renderType, false, gradientScale, gradientRotation, colorBlendMode);
            UpdateCollider(deco, solid, opacityCollision);
        }

        /// <summary>
        /// Updates the solid objects' collision.
        /// </summary>
        /// <param name="deco">If the object shouldn't damage players.</param>
        /// <param name="solid">If players can't pass through the object.</param>
        /// <param name="opacityCollision">If opacity of the object changes collision.</param>
        public void UpdateCollider(bool deco, bool solid, bool opacityCollision)
        {
            this.deco = deco;
            this.solid = solid;
            this.opacityCollision = opacityCollision;

            if (!collider)
                return;

            collider.enabled = HasCollision;
            collider.tag = deco ? Tags.HELPER : Tags.OBJECTS;

            collider.isTrigger = !solid;
        }

        /// <summary>
        /// Updates the objects' materials based on specific values.
        /// </summary>
        /// <param name="gradientType">Type of gradient to render.</param>
        public void UpdateRendering(int gradientType, int renderType, bool doubleSided = false, float gradientScale = 1f, float gradientRotation = 0f, int colorBlendMode = 0)
        {
            this.doubleSided = doubleSided;

            SetRenderType(renderType);

            isGradient = gradientType != 0;
            this.gradientType = gradientType;
            this.colorBlendMode = colorBlendMode;

            SetMaterial(LegacyResources.GetObjectMaterial(doubleSided, gradientType, colorBlendMode));

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
            primaryColor = color;
            float a = color.a * opacity;
            material?.SetColor(new Color(color.r, color.g, color.b, a));
            opacityCollide = !opacityCollision || a > 0.99f;
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
                primaryColor = color2;
                secondaryColor = color;
                material.SetColor("_Color", new Color(color2.r, color2.g, color2.b, color2.a * opacity));
                material.SetColor("_ColorSecondary", new Color(color.r, color.g, color.b, color.a * opacity));
            }
            else
            {
                primaryColor = color;
                secondaryColor = color2;
                material.SetColor("_Color", new Color(color.r, color.g, color.b, color.a * opacity));
                material.SetColor("_ColorSecondary", new Color(color2.r, color2.g, color2.b, color2.a * opacity));
            }

            opacityCollide = !opacityCollision || color.a + color2.a > 1.99f;
        }

        public override void SetPrimaryColor(Color color) => material.color = color;

        public override void SetSecondaryColor(Color color)
        {
            if (isGradient)
                material.SetColor("_ColorSecondary", color);
        }

        public override Color GetPrimaryColor() => primaryColor; // get cached primary color due to the opacity value for helpers.

        public override Color GetSecondaryColor() => !isGradient ? LSColors.pink500 : secondaryColor; // get cached secondary color due to the opacity value for helpers.

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
            gradientScale = scale;
            gradientRotation = rotation;

            if (!isGradient)
                return;

            material.SetFloat("_Scale", scale);
            if (IsLinear)
                material.SetFloat("_Rotation", rotation);
        }

        /// <summary>
        /// Updates the renderer materials.
        /// </summary>
        public void UpdateMaterials() => SetMaterial(material);

        /// <summary>
        /// Sets the material of the visual object.
        /// </summary>
        /// <param name="material">Material to set.</param>
        public void SetMaterial(Material material)
        {
            if (hasEditorOutline)
            {
                if (hasOutline)
                    renderer.materials = new Material[3]
                    {
                        material,
                        outlineType switch
                        {
                            1 => LegacyResources.outlineBehindMaterial,
                            _ => LegacyResources.outlineMaterial,
                        },
                        LegacyResources.editorOutlineMaterial,
                    };
                else
                    renderer.materials = new Material[2]
                    {
                        material,
                        LegacyResources.editorOutlineMaterial,
                    };
            }
            else
            {
                if (hasOutline)
                    renderer.materials = new Material[2]
                    {
                        material,
                        outlineType switch
                        {
                            1 => LegacyResources.outlineBehindMaterial,
                            _ => LegacyResources.outlineMaterial,
                        },
                    };
                else
                    renderer.materials = new Material[1]
                    {
                        material,
                    };
            }

            renderer.material = material;
            this.material = renderer.material;

            SetOutline(outlineData.color, outlineData.width);
            SetEditorOutline(editorOutlineData.color, editorOutlineData.width);
        }

        /// <summary>
        /// Gets the outline material.
        /// </summary>
        /// <returns>Returns the outline material from the renderer.</returns>
        public Material GetOutlineMaterial() => renderer.materials.GetAtOrDefault(1, null);

        /// <summary>
        /// Gets the outline material used for the editor.
        /// </summary>
        /// <returns>Returns the outline material from the renderer.</returns>
        public Material GetEditorOutlineMaterial() => renderer.materials.GetAtOrDefault(hasOutline ? 2 : 1, null);

        /// <summary>
        /// Adds an outline to the object. If the object already has an outline, don't do anything.
        /// </summary>
        public void AddOutline(int outlineType)
        {
            if (hasOutline || this.outlineType != outlineType)
                return;

            hasOutline = true;
            this.outlineType = outlineType;
            UpdateMaterials();
        }

        /// <summary>
        /// Removes the outline from the object, if it has an outline.
        /// </summary>
        public void RemoveOutline()
        {
            if (!hasOutline)
                return;

            hasOutline = false;
            UpdateMaterials();
        }

        /// <summary>
        /// Adds an outline to the object. If the object already has an outline, don't do anything.
        /// </summary>
        public void AddEditorOutline()
        {
            if (hasEditorOutline)
                return;

            hasEditorOutline = true;
            UpdateMaterials();
        }

        /// <summary>
        /// Removes the outline from the object, if it has an outline.
        /// </summary>
        public void RemoveEditorOutline()
        {
            if (!hasEditorOutline)
                return;

            hasEditorOutline = false;
            UpdateMaterials();
        }

        /// <summary>
        /// Sets the outline values.
        /// </summary>
        /// <param name="outlineColor">Outline color to set.</param>
        /// <param name="outlineWidth">Outline width to set.</param>
        public void SetOutline(Color outlineColor, float outlineWidth)
        {
            outlineData.color = outlineColor;
            outlineData.width = outlineWidth;
            outlineData.type = outlineType;

            if (!hasOutline)
                return;

            var material = GetOutlineMaterial();
            if (!material)
                return;

            material.SetColor("_OutlineColor", outlineColor);
            material.SetFloat("_OutlineWidth", outlineWidth);
        }

        /// <summary>
        /// Sets the outline values.
        /// </summary>
        /// <param name="outlineColor">Outline color to set.</param>
        /// <param name="outlineWidth">Outline width to set.</param>
        public void SetEditorOutline(Color outlineColor, float outlineWidth)
        {
            editorOutlineData.color = outlineColor;
            editorOutlineData.width = outlineWidth;
            editorOutlineData.type = 0;

            if (!hasEditorOutline)
                return;

            var material = GetOutlineMaterial();
            if (!material)
                return;

            material.SetColor("_OutlineColor", outlineColor);
            material.SetFloat("_OutlineWidth", outlineWidth);
        }

        public override void Clear()
        {
            base.Clear();
            material = null;
        }
    }
}
