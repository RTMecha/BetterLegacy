using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    // TODO: consider implementing this
    public class OnLevelRestart : ModifierTriggerBase
    {
        #region Constructors

        public OnLevelRestart()
        {

        }

        #endregion

        #region Values

        public override string Name => "onLevelRestart";

        public override ModifierCategoryType Category => ModifierCategoryType.Level;

        public override bool DisplayInEditor => false;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => false;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
