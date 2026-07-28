using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LocalVariableContains : ModifierTriggerBase
    {
        #region Constructors

        public LocalVariableContains() => SetupModifier("0", "0");

        #endregion

        #region Values

        public override string Name => "localVariableContains";

        public override CategoryType Category => CategoryType.Modifier;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => modifierLoop.variables.TryGetValue(modifier.GetValue(0), out string result) && result.Contains(modifier.GetValue(1, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Contains", 1);
        }

        #endregion
    }
}
