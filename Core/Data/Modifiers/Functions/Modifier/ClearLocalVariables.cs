using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ClearLocalVariables : ModifierActionBase
    {
        #region Constructors

        public ClearLocalVariables() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "clearLocalVariables";

        public override ModifierCategoryType Category => ModifierCategoryType.Modifier;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop) => modifierLoop.variables.Clear();

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
