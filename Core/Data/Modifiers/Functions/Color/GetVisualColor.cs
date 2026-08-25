using UnityEngine.UI;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetVisualColor : ModifierActionBase
    {
        public GetVisualColor(bool isGroup, bool isRGBA)
        {
            this.isGroup = isGroup;
            this.isRGBA = isRGBA;
            Name = "getVisualColor";
            if (isRGBA)
                Name += "RGBA";
            if (isGroup)
                Name += "Other";
            if (isRGBA)
                SetupModifier("VISUALCOLOR1R_VAR", "VISUALCOLOR1G_VAR", "VISUALCOLOR1B_VAR", "VISUALCOLOR1A_VAR", "VISUALCOLOR2R_VAR", "VISUALCOLOR2G_VAR", "VISUALCOLOR2B_VAR", "VISUALCOLOR2A_VAR");
            else
                SetupModifier("VISUALCOLOR1_VAR", "VISUALCOLOR2_VAR");
            if (isGroup)
                Modifier.values.Add("Object Group");
            IsGroup = isGroup;
        }

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Color;

        public override ModifierCompatibility Compatibility => isGroup ? base.Compatibility : ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject();

        readonly bool isGroup;
        readonly bool isRGBA;

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!TryGetBeatmapObject(modifier, modifierLoop, isGroup, isRGBA ? 8 : 2, out BeatmapObject beatmapObject))
                return;
            var colors = beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is SolidObject solidObject ? solidObject.GetColors() : beatmapObject.GetColors();
            if (isRGBA)
            {
                var startColorRName = FormatStringVariables(modifier.GetValue(0), modifierLoop.variables);
                var startColorGName = FormatStringVariables(modifier.GetValue(1), modifierLoop.variables);
                var startColorBName = FormatStringVariables(modifier.GetValue(2), modifierLoop.variables);
                var startColorAName = FormatStringVariables(modifier.GetValue(3), modifierLoop.variables);
                var endColorRName = FormatStringVariables(modifier.GetValue(4), modifierLoop.variables);
                var endColorGName = FormatStringVariables(modifier.GetValue(5), modifierLoop.variables);
                var endColorBName = FormatStringVariables(modifier.GetValue(6), modifierLoop.variables);
                var endColorAName = FormatStringVariables(modifier.GetValue(7), modifierLoop.variables);
                if (!string.IsNullOrEmpty(startColorRName))
                    modifierLoop.variables[startColorRName] = colors.startColor.r.ToString();
                if (!string.IsNullOrEmpty(startColorGName))
                    modifierLoop.variables[startColorGName] = colors.startColor.g.ToString();
                if (!string.IsNullOrEmpty(startColorBName))
                    modifierLoop.variables[startColorBName] = colors.startColor.b.ToString();
                if (!string.IsNullOrEmpty(startColorAName))
                    modifierLoop.variables[startColorAName] = colors.startColor.a.ToString();
                if (!string.IsNullOrEmpty(endColorRName))
                    modifierLoop.variables[endColorRName] = colors.endColor.r.ToString();
                if (!string.IsNullOrEmpty(endColorGName))
                    modifierLoop.variables[endColorGName] = colors.endColor.g.ToString();
                if (!string.IsNullOrEmpty(endColorBName))
                    modifierLoop.variables[endColorBName] = colors.endColor.b.ToString();
                if (!string.IsNullOrEmpty(endColorAName))
                    modifierLoop.variables[endColorAName] = colors.endColor.a.ToString();
                return;
            }
            var startColorName = FormatStringVariables(modifier.GetValue(0), modifierLoop.variables);
            var endColorName = FormatStringVariables(modifier.GetValue(1), modifierLoop.variables);
            if (!string.IsNullOrEmpty(startColorName))
                modifierLoop.variables[startColorName] = RTColors.ColorToHexOptional(colors.startColor);
            if (!string.IsNullOrEmpty(endColorName))
                modifierLoop.variables[endColorName] = RTColors.ColorToHexOptional(colors.endColor);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", isRGBA ? 8 : 2);
            }

            if (isRGBA)
            {
                modifierCard.StringGenerator(modifier, reference, "Color 1 R Var Name", 0, renderVariables: false);
                modifierCard.StringGenerator(modifier, reference, "Color 1 G Var Name", 1, renderVariables: false);
                modifierCard.StringGenerator(modifier, reference, "Color 1 B Var Name", 2, renderVariables: false);
                modifierCard.StringGenerator(modifier, reference, "Color 1 A Var Name", 3, renderVariables: false);
                modifierCard.StringGenerator(modifier, reference, "Color 2 R Var Name", 4, renderVariables: false);
                modifierCard.StringGenerator(modifier, reference, "Color 2 G Var Name", 5, renderVariables: false);
                modifierCard.StringGenerator(modifier, reference, "Color 2 B Var Name", 6, renderVariables: false);
                modifierCard.StringGenerator(modifier, reference, "Color 2 A Var Name", 7, renderVariables: false);
                return;
            }

            modifierCard.StringGenerator(modifier, reference, "Color 1 Var Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Color 2 Var Name", 1, renderVariables: false);
        }
    }
}
