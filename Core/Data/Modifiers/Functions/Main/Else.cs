using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class Else : ModifierTriggerBase
    {
        #region Constructors

        public Else()
        {
            SetupModifier();
            Modifier.collapse = true;
        }

        #endregion

        #region Values

        public override string Name => "else";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => true;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
