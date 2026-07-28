using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class OnLevelRewind : ModifierTriggerBase
    {
        #region Constructors

        public OnLevelRewind() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "onLevelRewind";

        public override CategoryType Category => CategoryType.Level;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => ProjectArrhythmia.State.Reversing;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
