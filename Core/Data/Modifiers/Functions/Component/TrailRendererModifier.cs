using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class TrailRendererModifier : ModifierActionBase
    {
        #region Constructors

        public TrailRendererModifier(bool isHex)
        {
            this.isHex = isHex;
            Name = "trailRenderer";
            if (isHex)
                Name += "Hex";
            SetupModifier(isHex ?
                new string[]
                {
                    "1", // Time
                    "1", // Start Width
                    "0", // End Width
                    RTColors.WHITE_HEX_CODE, // Start Color
                    RTColors.WHITE_HEX_CODE + "00", // End Color
                    "0", // If this was removed it would break something since i think a migration code would be far too much for an update
                    "True", // Scale Affected
                } :
                new string[]
                {
                    "1", // Time
                    "1", // Start Width
                    "0", // End Width
                    "0", // Start Color
                    "1", // Start Opacity
                    "0", // End Color
                    "0", // End Opacity
                    "0", // same here
                    "True", // Scale Affected
                });
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Component;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isHex;

        #endregion

        #region Functions

        TrailRenderer InitTrailRenderer(GameObject gameObject)
        {
            var trailObject = new GameObject("Trail Renderer");
            trailObject.layer = gameObject.layer;
            trailObject.transform.SetParent(gameObject.transform);
            trailObject.transform.localPosition = Vector3.zero;
            trailObject.transform.localRotation = Quaternion.identity;
            var trailRenderer = trailObject.AddComponent<TrailRenderer>();
            trailRenderer.material = LegacyResources.trailMaterial;
            trailRenderer.material.color = Color.white;
            trailRenderer.alignment = LineAlignment.TransformZ;
            return trailRenderer;
        }

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gameObject = runtimeObject.visualObject.gameObject;
            var tr = modifier.GetResultOrDefault(() => InitTrailRenderer(gameObject));
            if (!tr)
                tr = InitTrailRenderer(gameObject);

            var lossyScale = gameObject.transform.lossyScale;
            tr.transform.localScale = new Vector3(
                Mathf.Abs(lossyScale.x) > 0.0001f ? 1f / lossyScale.x : 1f,
                Mathf.Abs(lossyScale.y) > 0.0001f ? 1f / lossyScale.y : 1f,
                Mathf.Abs(lossyScale.z) > 0.0001f ? 1f / lossyScale.z : 1f);

            tr.time = modifier.GetFloat(0, 1f, modifierLoop.variables);
            tr.emitting = !(lossyScale.x < 0.001f && lossyScale.x > -0.001f || lossyScale.y < 0.001f && lossyScale.y > -0.001f) && gameObject.activeSelf && gameObject.activeInHierarchy;

            var affectedByScale = modifier.GetBool(isHex ? 6 : 8, true, modifierLoop.variables);
            var t = affectedByScale ? lossyScale.magnitude * 0.576635f : 1f;
            tr.startWidth = modifier.GetFloat(1, 1f, modifierLoop.variables) * t;
            tr.endWidth = modifier.GetFloat(2, 1f, modifierLoop.variables) * t;

            if (isHex)
            {
                tr.startColor = RTColors.HexToColor(FormatStringVariables(modifier.GetValue(3, modifierLoop.variables), modifierLoop.variables));
                tr.endColor = RTColors.HexToColor(FormatStringVariables(modifier.GetValue(4, modifierLoop.variables), modifierLoop.variables));
            }
            else
            {
                var beatmapTheme = CoreHelper.CurrentBeatmapTheme;
                tr.startColor = RTColors.FadeColor(beatmapTheme.GetObjColor(modifier.GetInt(3, 0, modifierLoop.variables)), modifier.GetFloat(4, 1f, modifierLoop.variables));
                tr.endColor = RTColors.FadeColor(beatmapTheme.GetObjColor(modifier.GetInt(5, 0, modifierLoop.variables)), modifier.GetFloat(6, 1f, modifierLoop.variables));
            }

            if (runtimeObject.visualObject.HasStencilProperties && runtimeObject.visualObject is SolidObject solidObject)
                solidObject.GetStencilProperties().ApplyToMaterial(tr.material);
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.TryGetResult(out TrailRenderer trailRenderer) && trailRenderer)
                trailRenderer.emitting = false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Time", 0, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Start Width", 1, 1f);
            modifierCard.SingleGenerator(modifier, reference, "End Width", 2, 0f);
            if (isHex)
            {
                modifierCard.StringGenerator(modifier, reference, "Start Color", 3);
                modifierCard.StringGenerator(modifier, reference, "End Color", 4);
                modifierCard.BoolGenerator(modifier, reference, "Affected by Scale", 6, true);
                return;
            }
            modifierCard.ColorGenerator(modifier, reference, "Start Color", 3);
            modifierCard.SingleGenerator(modifier, reference, "Start Opacity", 4, 1f);
            modifierCard.ColorGenerator(modifier, reference, "End Color", 5);
            modifierCard.SingleGenerator(modifier, reference, "End Opacity", 6, 0f);
            modifierCard.BoolGenerator(modifier, reference, "Affected by Scale", 8, true);
        }

        public override void OnRemoveCache(Modifier modifier)
        {
            if (!modifier.TryGetResult(out TrailRenderer trailRenderer))
                return;
            if (trailRenderer)
                CoreHelper.Destroy(trailRenderer.gameObject);
            modifier.Result = default;
        }

        #endregion
    }
}
