using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetMixedColors : ModifierVariableBase
    {
        #region Constructors

        public GetMixedColors() => SetupModifier("MIXEDCOLORS_VAR");

        #endregion

        #region Values

        public override string Name => "getMixedColors";

        public override ModifierCategoryType Category => ModifierCategoryType.Color;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            var colors = new List<Color>();
            for (int i = 1; i < modifier.values.Count; i++)
                colors.Add(RTColors.HexToColor(FormatStringVariables(modifier.GetValue(i, modifierLoop.variables), modifierLoop.variables)));
            return RTColors.ColorToHexOptional(RTColors.MixColors(colors)).ToString();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

            int a = 0;
            for (int i = 1; i < modifier.values.Count; i++)
            {
                int groupIndex = i;
                var label = modifierCard.LabelGenerator($"- Color {a + 1}");

                modifierCard.DeleteGenerator(modifier, reference, label.transform, () => modifier.values.RemoveAt(groupIndex));

                modifierCard.StringGenerator(modifier, reference, "Color Hex Code", i);
                a++;
            }

            modifierCard.AddGenerator(modifier, reference, "Add Color Value", () =>
            {
                modifier.values.Add(RTColors.ColorToHexOptional(RTColors.errorColor));
            });
        }

        #endregion
    }
}
