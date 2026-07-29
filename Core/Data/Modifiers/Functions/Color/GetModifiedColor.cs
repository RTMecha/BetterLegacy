using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetModifiedColor : ModifierVariableBase
    {
        #region Constructors

        public GetModifiedColor() => SetupModifier("MODIFIEDCOLORS_VAR", "FFFFFF", "1", "0", "0", "0");

        #endregion

        #region Values

        public override string Name => "getModifiedColor";

        public override ModifierCategoryType Category => ModifierCategoryType.Color;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => RTColors.ColorToHexOptional(RTColors.ChangeColorHSVA(
                color: RTColors.HexToColor(FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables)),
                hue: modifier.GetFloat(3, 0f, modifierLoop.variables),
                sat: modifier.GetFloat(4, 0f, modifierLoop.variables),
                val: modifier.GetFloat(5, 0f, modifierLoop.variables),
                opacity: modifier.GetFloat(2, 1f, modifierLoop.variables)));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Hex Color", 1);

            modifierCard.SingleGenerator(modifier, reference, "Opacity", 2, 1f, max: 1f);
            modifierCard.SingleGenerator(modifier, reference, "Hue", 3);
            modifierCard.SingleGenerator(modifier, reference, "Saturation", 4);
            modifierCard.SingleGenerator(modifier, reference, "Value", 5);
        }

        #endregion
    }
}
