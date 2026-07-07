using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

using LSFunctions;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects.Visual
{
    /// <summary>
    /// Class for regular shape objects.
    /// </summary>
    public class SolidObject : VisualObject
    {
        public SolidObject(GameObject gameObject, float opacity, bool deco, bool solid, int renderType, bool opacityCollision, int gradientType, float gradientScale, float gradientRotation, int colorBlendMode, ParticleSystemData particleSystemData)
        {
            this.gameObject = gameObject;

            this.opacity = opacity;

            renderer = gameObject.GetComponent<Renderer>();
            renderer.enabled = true;

            collider = gameObject.GetComponent<Collider2D>();

            UpdateRendering(gradientType, renderType, false, gradientScale, gradientRotation, colorBlendMode);
            UpdateCollider(deco, solid, opacityCollision);

            particleSystemDataCache = particleSystemData;
        }
        
        #region Values

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

        public override bool HasCollision => (forceCollisionEnabled || solid || !RTBeatmap.Current.Invincible && !deco || ProjectArrhythmia.State.IsEditing && EditorConfig.Instance.SelectObjectsInPreview.Value) && colliderEnabled && opacityCollide;

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

        public ParticleSystemData particleSystemDataCache;

        public bool emittingParticles;

        Color primaryColor;
        Color secondaryColor;

        const float PARTICLE_SCRUB_STEP = 0.06666667f;
        const float PARTICLE_TIME_SYNC_TOLERANCE = 0.05f;

        #endregion

        #region Functions

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
            UpdateCollider();
        }

        /// <summary>
        /// Updates the solid objects' collision.
        /// </summary>
        public void UpdateCollider()
        {
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

        public override void Clear()
        {
            base.Clear();
            material = null;
        }

        #region Color

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

        #endregion

        #region Materials

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
        /// Resets the stencil properties.
        /// </summary>
        public void ResetStencil()
        {
            HasStencilProperties = false;
            SetStencil(CompareFunction.Always, StencilOp.Keep, StencilOp.Keep, StencilOp.Keep, 0, 255, 255);
        }

        /// <summary>
        /// Sets the stencil properties.
        /// </summary>
        /// <param name="comparison">Compare function.</param>
        /// <param name="pass">Stencil pass operation.</param>
        /// <param name="fail">Stencil fail operation.</param>
        /// <param name="zFail">Stencil Z fail operation.</param>
        /// <param name="id">Stencil ID.</param>
        /// <param name="writeMask">Stencil write mask.</param>
        /// <param name="readMask">Stencil read mask.</param>
        public void SetStencil(CompareFunction comparison, StencilOp pass, StencilOp fail, StencilOp zFail, byte id, byte writeMask, byte readMask)
        {
            HasStencilProperties = true;
            StencilProperties.ApplyToMaterial(material, comparison, pass, fail, zFail, id, writeMask, readMask);
        }

        /// <summary>
        /// Gets the <see cref="StencilProperties"/> from the material.
        /// </summary>
        /// <returns>Returns the <see cref="StencilProperties"/> from the material.</returns>
        public StencilProperties GetStencilProperties() => new StencilProperties
        {
            compare = (CompareFunction)material.GetFloat("_StencilComp"),
            id = (byte)material.GetFloat("_Stencil"),
            pass = (StencilOp)material.GetFloat("_StencilOp"),
            fail = (StencilOp)material.GetFloat("_StencilFail"),
            zFail = (StencilOp)material.GetFloat("_StencilZFail"),
            writeMask = (byte)material.GetFloat("_StencilWriteMask"),
            readMask = (byte)material.GetFloat("_StencilReadMask"),
        };

        #endregion

        #region Outline

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

        #endregion

        #region Particles

        // genuinely hate dealing with particles

        public override void SetupParticles(Mesh particleMesh, BeatmapObject beatmapObject)
        {
            if (!particleSystemDataCache)
                return;

            particleSystem = gameObject.GetComponent<ParticleSystem>();
            if (!particleSystem)
                return;

            particleSystemRenderer = gameObject.GetComponent<ParticleSystemRenderer>();
            StopParticles(ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.useAutoRandomSeed = false;
            particleSystem.randomSeed = (uint)RandomHelper.GetHash(beatmapObject.id, RandomHelper.CurrentSeed);

            var main = particleSystem.main;
            main.simulationSpace = (particleSystemDataCache.worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local);
            main.playOnAwake = false;

            var shape = particleSystem.shape;
            if (particleSystemDataCache.emitterShapeType == ParticleSystemData.EmitterShapeType.Circle)
            {
                shape.shapeType = ParticleSystemShapeType.Circle;
                float num = Mathf.Clamp(particleSystemDataCache.emitterArc, 0f, 360f);
                shape.arc = num;
                if (Mathf.Approximately(num, 0f) || Mathf.Approximately(num, 360f))
                {
                    float num2 = (Mathf.Approximately(num, 0f) ? 0.0001f : 359.9999f);
                    shape.arc = num2;
                    shape.arc = num; // the hell? it was like this in the vanilla code
                }
                shape.radiusThickness = Mathf.Clamp01(particleSystemDataCache.emitterRadius);
            }
            else
                shape.shapeType = ParticleSystemShapeType.Box;
            if (particleSystemRenderer)
            {
                particleSystemRenderer.renderMode = particleMesh != null ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard;
                particleSystemRenderer.mesh = particleMesh;

                particleSystemRenderer.alignment = particleSystemDataCache.worldSpace ? ParticleSystemRenderSpace.World : ParticleSystemRenderSpace.Local;
                main.scalingMode = particleSystemDataCache.worldSpace ? ParticleSystemScalingMode.Local : ParticleSystemScalingMode.Hierarchy;
            }
            main.startSpeed = particleSystemDataCache.startSpeed;
            var emission = particleSystem.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(particleSystemDataCache.spawnRatePerSecond);
            emission.rateOverDistance = new ParticleSystem.MinMaxCurve(particleSystemDataCache.spawnRatePerUnit);
            emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
            SetupParticlesTimeline(beatmapObject);
        }

        public override void SetupParticlesTimeline(BeatmapObject beatmapObject)
        {
            if (!particleSystem)
                return;
            var particlesSpawnDuration = beatmapObject.ParticlesSpawnDuration;
            var main = particleSystem.main;
            main.startLifetime = particlesSpawnDuration;
            main.duration = beatmapObject.SpawnDuration;
            main.loop = false;
            main.startRotation = (beatmapObject.events != null && beatmapObject.events.Count > 2 ? beatmapObject.events[2][0].GetSecondaryValue(0) : 0f) * 0.017453292f;
            StopParticles(ParticleSystemStopBehavior.StopEmittingAndClear);
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(1f, beatmapObject.events[0].ToAnimationCurve(2, 0, 0f, particlesSpawnDuration, null, true));
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, beatmapObject.events[0].ToAnimationCurve(2, 1, 0f, particlesSpawnDuration, null, true));
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, AnimationCurve.Constant(0f, 1f, 0f));
            velocityOverLifetime.xMultiplier = 1f;
            velocityOverLifetime.yMultiplier = 1f;
            velocityOverLifetime.zMultiplier = 0f;
            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.separateAxes = true;
            sizeOverLifetime.x = new ParticleSystem.MinMaxCurve(1f, beatmapObject.events[1].ToAnimationCurve(2, 0, 1f, particlesSpawnDuration, null, false));
            sizeOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, beatmapObject.events[1].ToAnimationCurve(2, 1, 1f, particlesSpawnDuration, null, false));
            sizeOverLifetime.z = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0f, 1f, 1f));
            sizeOverLifetime.xMultiplier = 1f;
            sizeOverLifetime.yMultiplier = 1f;
            sizeOverLifetime.zMultiplier = 1f;
            var rotationOverLifetime = particleSystem.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.separateAxes = true;
            rotationOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, AnimationCurve.Constant(0f, 1f, 0f));
            rotationOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, AnimationCurve.Constant(0f, 1f, 0f));
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(1f, beatmapObject.events[2].ToAnimationCurve(2, 0, 0f, particlesSpawnDuration, (float degrees) => degrees * 0.017453292f, true));
            rotationOverLifetime.xMultiplier = 0f;
            rotationOverLifetime.yMultiplier = 0f;
            rotationOverLifetime.zMultiplier = 1f;
        }

        public override void InterpolateParticles(float t)
        {
            if (!particleSystemDataCache || !particleSystem)
                return;

            var pitch = AudioManager.inst.CurrentAudioSource.pitch;
            var main = particleSystem.main;
            var emission = particleSystem.emission;
            emission.enabled = emittingParticles;
            var resync = RTMath.Distance(particleSystem.time, t) > PARTICLE_TIME_SYNC_TOLERANCE;
            if (resync)
            {
                main.simulationSpeed = 1f;
                if (t > PARTICLE_SCRUB_STEP)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var calc = 0f;
                    var start = true;
                    while (calc < t)
                    {
                        var particleTime = Mathf.Min(PARTICLE_SCRUB_STEP, t - calc);
                        calc += particleTime;
                        particleSystem.Simulate(particleTime, true, start);
                        start = false;
                    }
                    // this approach can cause an immense amount of lag but unfortunately this is how the official game handles it.
                }
                else
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particleSystem.Simulate(t, true, true, true);
                }
            }
            main.simulationSpeed = Mathf.Max(0f, pitch);
            if (!particleSystem.isPlaying || resync)
                particleSystem.Play(true);
        }

        public void StartParticles() => emittingParticles = true;

        public void StopParticles(ParticleSystemStopBehavior stopBehavior = ParticleSystemStopBehavior.StopEmitting)
        {
            emittingParticles = false;
            if (!particleSystem || particleSystem.isStopped && particleSystem.particleCount <= 0)
                return;
            particleSystem.Stop(true, stopBehavior);
        }

        public void ResetParticles()
        {
            emittingParticles = false;
            if (!particleSystem)
                return;
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Simulate(0f, true, true, true);
            particleSystem.Pause(true);
        }

        #endregion

        #endregion
    }
}
