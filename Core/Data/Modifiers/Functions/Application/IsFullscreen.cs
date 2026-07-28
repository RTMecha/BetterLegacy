using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class IsFullscreen : ModifierTriggerBase
    {
        #region Constructors

        public IsFullscreen() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "isFullscreen";

        public override CategoryType Category => CategoryType.Application;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => UnityEngine.Screen.fullScreen;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
