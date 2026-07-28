using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class IsFocused : ModifierTriggerBase
    {
        #region Constructors

        public IsFocused() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "isFocused";

        public override CategoryType Category => CategoryType.Application;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => UnityEngine.Application.isFocused;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
