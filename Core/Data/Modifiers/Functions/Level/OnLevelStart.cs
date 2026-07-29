using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class OnLevelStart : ModifierTriggerBase
    {
        #region Constructors

        public OnLevelStart() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "onLevelStart";

        public override ModifierCategoryType Category => ModifierCategoryType.Level;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => RTBeatmap.Current && RTBeatmap.Current.LevelStarted;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
