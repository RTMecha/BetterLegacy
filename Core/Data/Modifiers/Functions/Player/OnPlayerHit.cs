using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class OnPlayerHit : ModifierTriggerBase
    {
        #region Constructors

        public OnPlayerHit() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "onPlayerHit";

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => base.Compatibility;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => RTBeatmap.Current.playerHit;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
