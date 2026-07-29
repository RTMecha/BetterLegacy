using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetIncrementFloat : ModifierVariableBase
    {
        #region Constructors

        public GetIncrementFloat() => SetupModifier("FLOAT_VAR", "0", "1");

        #endregion

        #region Values

        public override string Name => "getIncrementFloat";

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => (modifier.GetFloat(1, 0f, modifierLoop.variables) + modifier.GetFloat(2, 0f, modifierLoop.variables)).ToString();

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.SingleGenerator(modifier, reference, "Value", 1, 0);
            modifierCard.SingleGenerator(modifier, reference, "Increment", 2, 0);
        }

        #endregion
    }
}
