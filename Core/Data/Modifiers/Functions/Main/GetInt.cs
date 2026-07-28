using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetInt : ModifierVariableBase
    {
        #region Constructors

        public GetInt() => SetupModifier("INT_VAR", "0");

        #endregion

        #region Values

        public override string Name => "getInt";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => modifier.GetInt(1, 0, modifierLoop.variables).ToString();

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.IntegerGenerator(modifier, reference, "Value", 1, 0);
        }

        #endregion
    }
}
