using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetParsedString : ModifierVariableBase
    {
        #region Constructors

        public GetParsedString() => SetupModifier("PARSEDSTRING_VAR", "text");

        #endregion

        #region Values

        public override string Name => "getParsedString";

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => RTString.ParseText(FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Value", 1);
        }

        #endregion
    }
}
