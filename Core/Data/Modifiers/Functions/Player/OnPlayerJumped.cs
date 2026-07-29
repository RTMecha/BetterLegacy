using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class OnPlayerJumped : ModifierTriggerBase
    {
        #region Constructors

        public OnPlayerJumped() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "onPlayerJumped";

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => base.Compatibility;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => RTBeatmap.Current.playerJumped;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
