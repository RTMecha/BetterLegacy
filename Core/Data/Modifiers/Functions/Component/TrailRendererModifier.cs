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
                    "0", // Alignment
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
                    "0", // Alignment
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

            if (!beatmapObject.trailRenderer)
            {
                beatmapObject.trailRenderer = gameObject.GetOrAddComponent<TrailRenderer>();

                beatmapObject.trailRenderer.material = LegacyResources.trailMaterial;
                beatmapObject.trailRenderer.material.color = Color.white;
            }

            var tr = beatmapObject.trailRenderer;

            var alignment = Parser.TryParse(modifier.GetValue(isHex ? 5 : 7, modifierLoop.variables), true, LineAlignment.View);
            if (tr.alignment != alignment)
                tr.alignment = alignment;
            tr.time = modifier.GetFloat(0, 1f, modifierLoop.variables);
            tr.emitting = !(gameObject.transform.lossyScale.x < 0.001f && gameObject.transform.lossyScale.x > -0.001f || gameObject.transform.lossyScale.y < 0.001f && gameObject.transform.lossyScale.y > -0.001f) && gameObject.activeSelf && gameObject.activeInHierarchy;

            var t = gameObject.transform.lossyScale.magnitude * 0.576635f;
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
            if (modifierLoop.reference is BeatmapObject beatmapObject && beatmapObject.trailRenderer)
                beatmapObject.trailRenderer.emitting = false;
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
                modifierCard.DropdownGenerator(modifier, reference, "Alignment", 5, CoreHelper.ToOptionData<LineAlignment>());
                return;
            }
            modifierCard.ColorGenerator(modifier, reference, "Start Color", 3);
            modifierCard.SingleGenerator(modifier, reference, "Start Opacity", 4, 1f);
            modifierCard.ColorGenerator(modifier, reference, "End Color", 5);
            modifierCard.SingleGenerator(modifier, reference, "End Opacity", 6, 0f);
            modifierCard.DropdownGenerator(modifier, reference, "Alignment", 7, CoreHelper.ToOptionData<LineAlignment>());
        }

        #endregion
    }
}
