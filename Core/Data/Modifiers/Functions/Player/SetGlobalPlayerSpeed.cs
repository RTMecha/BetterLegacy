using BetterLegacy.Core.Components.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetGlobalPlayerSpeed : ModifierActionBase
    {
        #region Constructors

        public SetGlobalPlayerSpeed() => SetupModifier("1");

        #endregion

        #region Values

        public override string Name => "setGlobalPlayerSpeed";

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop) => RTPlayer.SpeedMultiplier = modifier.GetFloat(0, 1f, modifierLoop.variables);

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Global Speed", 0, 1f);
        }

        #endregion
    }
}
