using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetColor : ModifierVariableBase
    {
        #region Constructors

        public GetColor() => SetupModifier("COLOR_VAR", "0");

        #endregion

        #region Values

        public override string Name => "getColor";

        public override CategoryType Category => CategoryType.Color;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => modifier.GetFloat(1, 0f, modifierLoop.variables).ToString();

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.ColorGenerator(modifier, reference, "Value", 1);
        }

        #endregion
    }
}
