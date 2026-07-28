using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class Break : ModifierTriggerBase
    {
        #region Constructors

        public Break()
        {
            SetupModifier();
            Modifier.collapse = true;
        }

        #endregion

        #region Values

        public override string Name => "break";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => true;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
