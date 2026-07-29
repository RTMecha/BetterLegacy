using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetFloatFromHexCode : ModifierVariableBase
    {
        #region Constructors

        public GetFloatFromHexCode() => SetupModifier("HEX_VAR", "FF");

        #endregion

        #region Values

        public override string Name => "getFloatFromHexCode";

        public override ModifierCategoryType Category => ModifierCategoryType.Color;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => RTColors.HexToFloat(FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables)).ToString();

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Hex Code", 1);
        }

        #endregion
    }
}
