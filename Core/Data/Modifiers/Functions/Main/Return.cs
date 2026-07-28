using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class Return : ModifierActionBase
    {
        #region Constructors

        public Return()
        {
            SetupModifier();
            Modifier.collapse = true;
        }

        #endregion

        #region Values

        public override string Name => "return";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop) { }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
