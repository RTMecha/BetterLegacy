using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetToggle : ModifierVariableBase
    {
        #region Constructors

        public GetToggle() => SetupModifier("TOGGLE_VAR", "False", "False");

        #endregion

        #region Values

        public override string Name => "getToggle";

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            var value = modifier.GetBool(1, false, modifierLoop.variables);
            if (modifier.GetBool(2, false, modifierLoop.variables))
                value = !value;
            return value.ToString();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.BoolGenerator(modifier, reference, "Value", 1, false);
            modifierCard.BoolGenerator(modifier, reference, "Invert Value", 2, false);
        }

        #endregion
    }
}
