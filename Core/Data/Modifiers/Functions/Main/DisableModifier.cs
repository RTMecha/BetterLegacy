using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class DisableModifier : ModifierTriggerBase
    {
        #region Constructors

        public DisableModifier() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "disableModifier";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => false;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
