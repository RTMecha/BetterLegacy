using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class RequireSignal : ModifierTriggerBase
    {
        #region Constructors

        public RequireSignal() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "requireSignal";

        public override CategoryType Category => CategoryType.Modifier;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => modifier.HasResult();

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
