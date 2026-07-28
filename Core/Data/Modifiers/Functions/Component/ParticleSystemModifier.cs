using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ParticleSystemModifier : ModifierActionBase
    {
        #region Constructors

        public ParticleSystemModifier(bool isHex)
        {
            this.isHex = isHex;
            Name = "particleSystem";
            if (isHex)
                Name += "Hex";
            SetupModifier(new string[]
            {
                "5", // Life Time
                "0", // Shape
                "0", // Shape Option
                isHex ? RTColors.WHITE_HEX_CODE : "0", // Color
                "1", // Start Opacity
                "0", // End Opacity
                "1", // Start Scale
                "0", // End Scale
                "0", // Rotation
                "5", // Speed
                "1", // Amount
                "1", // Duration
                "0", // Force X
                "0", // Force Y
                "True", // Emit Trail
                "90", // Angle
                "1", // Burst Count
            });
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Component;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isHex;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gameObject = runtimeObject.visualObject.gameObject;

            var particleSystemCache = modifier.GetResultOrDefault(() => GetCache(gameObject, modifier, modifierLoop));
            if (!particleSystemCache.ps)
            {
                particleSystemCache = GetCache(gameObject, modifier, modifierLoop);
                modifier.Result = particleSystemCache;
            }

            var ps = particleSystemCache.ps;
            var psr = particleSystemCache.psr;

            var psMain = ps.main;
            var psEmission = ps.emission;

            psMain.startSpeed = modifier.GetFloat(9, 5f, modifierLoop.variables);

            psMain.loop = modifier.constant;
            ps.emissionRate = modifier.GetFloat(10, 1f, modifierLoop.variables);
            //psEmission.burstCount = modifier.GetInt(16, 1, modifierLoop.variables);
            psMain.duration = modifier.GetFloat(11, 1f, modifierLoop.variables);

            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.zMultiplier = modifier.GetFloat(8, 0f, modifierLoop.variables);

            var forceOverLifetime = ps.forceOverLifetime;
            forceOverLifetime.xMultiplier = modifier.GetFloat(12, 0f, modifierLoop.variables);
            forceOverLifetime.yMultiplier = modifier.GetFloat(13, 0f, modifierLoop.variables);

            var particlesTrail = ps.trails;
            particlesTrail.enabled = modifier.GetBool(14, true, modifierLoop.variables);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var psCol = colorOverLifetime.color;

            float alphaStart = modifier.GetFloat(4, 1f, modifierLoop.variables);
            float alphaEnd = modifier.GetFloat(5, 0f, modifierLoop.variables);

            psCol.gradient.alphaKeys = new GradientAlphaKey[2] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaEnd, 1f) };
            psCol.gradient.colorKeys = new GradientColorKey[2] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) };
            psCol.gradient.mode = GradientMode.Blend;

            colorOverLifetime.color = psCol;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;

            var ssss = sizeOverLifetime.size;

            var sizeStart = modifier.GetFloat(6, 0f, modifierLoop.variables);
            var sizeEnd = modifier.GetFloat(7, 0f, modifierLoop.variables);

            var curve = new AnimationCurve(new Keyframe[2] { new Keyframe(0f, sizeStart), new Keyframe(1f, sizeEnd) });

            ssss.curve = curve;

            sizeOverLifetime.size = ssss;

            psMain.startLifetime = modifier.GetFloat(0, 1f, modifierLoop.variables);
            psEmission.enabled = !(gameObject.transform.lossyScale.x < 0.001f && gameObject.transform.lossyScale.x > -0.001f || gameObject.transform.lossyScale.y < 0.001f && gameObject.transform.lossyScale.y > -0.001f) && gameObject.activeSelf && gameObject.activeInHierarchy;

            psMain.startColor = isHex ? RTColors.HexToColor(FormatStringVariables(modifier.GetValue(3, modifierLoop.variables), modifierLoop.variables)) : CoreHelper.CurrentBeatmapTheme.GetObjColor(modifier.GetInt(3, 0, modifierLoop.variables));

            var shape = ps.shape;
            shape.angle = modifier.GetFloat(15, 90f, modifierLoop.variables);

            if (!modifier.constant)
                RTLevel.Current.postTick.Enqueue(() => ps.Emit(modifier.GetInt(16, 1, modifierLoop.variables)));

            if (runtimeObject.visualObject.HasStencilProperties && runtimeObject.visualObject is SolidObject solidObject)
                solidObject.GetStencilProperties().ApplyToMaterial(psr.material);
        }

        Cache GetCache(GameObject gameObject, Modifier modifier, ModifierLoop modifierLoop)
        {
            //var solidObject = runtimeObject.visualObject as SolidObject;
            var ps = gameObject.GetOrAddComponent<ParticleSystem>();
            var psr = gameObject.GetComponent<ParticleSystemRenderer>();

            var s = modifier.GetInt(1, 0, modifierLoop.variables);
            var so = modifier.GetInt(2, 0, modifierLoop.variables);

            s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

            psr.mesh = ObjectManager.inst.objectPrefabs[s == 4 ? 0 : s == 6 ? 0 : s].options[so].GetComponentInChildren<MeshFilter>().mesh;

            psr.material = LegacyResources.trailMaterial;
            //psr.material = LegacyResources.GetObjectMaterial(solidObject && solidObject.doubleSided, solidObject?.gradientType ?? 0, solidObject?.colorBlendMode ?? 0);
            psr.material.color = Color.white;
            psr.trailMaterial = psr.material;
            psr.renderMode = ParticleSystemRenderMode.Mesh;

            var psMain = ps.main;

            psMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.separateAxes = true;
            rotationOverLifetime.xMultiplier = 0f;
            rotationOverLifetime.yMultiplier = 0f;

            var forceOverLifetime = ps.forceOverLifetime;
            forceOverLifetime.enabled = true;
            forceOverLifetime.space = ParticleSystemSimulationSpace.World;

            modifier.Result = ps;
            gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
            return new Cache(ps, psr);
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.TryGetResult(out Cache cache) && cache.ps)
            {
                var emission = cache.ps.emission;
                emission.enabled = false;
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Life Time", 0, 5f);
            modifierCard.DropdownGenerator(modifier, reference, "Shape", 1, ShapeManager.inst.Shapes2D.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), new List<bool>
            {
                false, // square
                false, // circle
                false, // triangle
                false, // arrow
                true, // text
                false, // hexagon
                true, // image
                false, // pentagon
                false, // misc
                true, // polygon
            },
            _val =>
            {
                var shapeType = (ShapeType)_val;
                if (shapeType == ShapeType.Text || shapeType == ShapeType.Image || shapeType == ShapeType.Polygon)
                    modifier.SetValue(1, "0");
                else
                    modifier.SetValue(1, _val.ToString());
                modifier.SetValue(2, "0");
                modifierCard.RenderModifier(reference);
                modifierCard.Update(modifier, reference);
            });
            var shape = modifier.GetInt(1, 0);
            modifierCard.DropdownGenerator(modifier, reference, "Shape Option", 2, ShapeManager.inst.Shapes2D[shape].shapes.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList(), null);
            if (isHex)
                modifierCard.StringGenerator(modifier, reference, "Color", 3);
            else
                modifierCard.ColorGenerator(modifier, reference, "Color", 3);
            modifierCard.SingleGenerator(modifier, reference, "Start Opacity", 4, 1f);
            modifierCard.SingleGenerator(modifier, reference, "End Opacity", 5, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Start Scale", 6, 1f);
            modifierCard.SingleGenerator(modifier, reference, "End Scale", 7, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Rotation", 8, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Speed", 9, 5f);
            modifierCard.SingleGenerator(modifier, reference, "Amount", 10, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Duration", 11, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Force X", 12, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Force Y", 13, 0f);
            modifierCard.BoolGenerator(modifier, reference, "Emit Trail", 14, false);
            modifierCard.SingleGenerator(modifier, reference, "Angle", 15, 0f);
            modifierCard.IntegerGenerator(modifier, reference, "Burst Count", 16, 0);
        }

        #endregion

        #region Sub Classes

        public class Cache
        {
            public Cache() { }

            public Cache(ParticleSystem ps, ParticleSystemRenderer psr)
            {
                this.ps = ps;
                this.psr = psr;
            }

            public ParticleSystem ps;
            public ParticleSystemRenderer psr;
        }

        #endregion
    }
}
