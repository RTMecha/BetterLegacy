using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetHexCodeFromFloat : ModifierVariableBase
    {
        #region Constructors

        public GetHexCodeFromFloat() => SetupModifier("HEX_VAR", "0");

        #endregion

        #region Values

        public override string Name => "getHexCodeFromFloat";

        public override ModifierCategoryType Category => ModifierCategoryType.Color;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => RTColors.FloatToHex(modifier.GetFloat(1, 1f, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.SingleGenerator(modifier, reference, "Value", 1, 0f, max: 1f);
        }

        #endregion
    }
}
