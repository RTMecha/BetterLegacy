using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetIncrementInt : ModifierVariableBase
    {
        #region Constructors

        public GetIncrementInt() => SetupModifier("INT_VAR", "0", "1");

        #endregion

        #region Values

        public override string Name => "getIncrementInt";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => (modifier.GetInt(1, 0, modifierLoop.variables) + modifier.GetInt(2, 0, modifierLoop.variables)).ToString();

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.SingleGenerator(modifier, reference, "Value", 1, 0);
            modifierCard.SingleGenerator(modifier, reference, "Increment", 2, 0);
        }

        #endregion
    }
}
