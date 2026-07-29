using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetLerpColor : ModifierVariableBase
    {
        #region Constructors

        public GetLerpColor(bool isAdd)
        {
            this.isAdd = isAdd;
            Name = isAdd ? "getAddColor" : "getLerpColor";
            SetupModifier(isAdd ? "ADDCOLOR_VAR" : "LERPCOLOR_VAR", "FFFFFF", "000000", "0.5");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        readonly bool isAdd;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            var a = RTColors.HexToColor(FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables));
            var b = RTColors.HexToColor(FormatStringVariables(modifier.GetValue(2, modifierLoop.variables), modifierLoop.variables));
            return isAdd ?
                RTColors.ColorToHexOptional(a + b * modifier.GetFloat(3, 1f, modifierLoop.variables)) :
                RTColors.ColorToHexOptional(RTMath.Lerp(a, b, modifier.GetFloat(3, 1f, modifierLoop.variables)));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Hex Color 1", 1);
            modifierCard.StringGenerator(modifier, reference, "Hex Color 2", 2);
            modifierCard.SingleGenerator(modifier, reference, isAdd ? "Add Amount" : "Multiply", 3);
        }

        #endregion
    }
}
