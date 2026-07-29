using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class OnPlayerBoosted : ModifierTriggerBase
    {
        #region Constructors

        public OnPlayerBoosted() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "onPlayerBoosted";

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => base.Compatibility;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => RTBeatmap.Current.playerBoosted;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
