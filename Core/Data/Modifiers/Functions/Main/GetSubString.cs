using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetSubString : ModifierVariableBase
    {
        #region Constructors

        public GetSubString() => SetupModifier("SUBSTRING_VAR", "0", "1");

        #endregion

        #region Values

        public override string Name => "getSubString";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            try
            {
                var str = FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables);
                return str.Substring(RTMath.Clamp(modifier.GetInt(2, 0, modifierLoop.variables), 0, str.Length), RTMath.Clamp(modifier.GetInt(3, 0, modifierLoop.variables), 0, str.Length));
            }
            catch
            {
                return null;
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.IntegerGenerator(modifier, reference, "Start Index", 1, 0);
            modifierCard.IntegerGenerator(modifier, reference, "Length", 2, 0);
        }

        #endregion
    }
}
