using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LocalVariableEndsWith : ModifierTriggerBase
    {
        #region Constructors

        public LocalVariableEndsWith() => SetupModifier("0", "0");

        #endregion

        #region Values

        public override string Name => "localVariableEndsWith";

        public override CategoryType Category => CategoryType.Modifier;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => modifierLoop.variables.TryGetValue(modifier.GetValue(0), out string result) && result.EndsWith(modifier.GetValue(1, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Ends With", 1);
        }

        #endregion
    }
}
