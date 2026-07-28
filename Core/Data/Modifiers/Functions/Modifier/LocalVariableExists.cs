using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LocalVariableExists : ModifierTriggerBase
    {
        #region Constructors

        public LocalVariableExists() => SetupModifier("CHECK_VAR");

        #endregion

        #region Values

        public override string Name => "localVariableExists";

        public override CategoryType Category => CategoryType.Modifier;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => modifierLoop.variables.ContainsKey(modifier.GetValue(0));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
        }

        #endregion
    }
}
