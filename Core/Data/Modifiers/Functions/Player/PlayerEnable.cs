using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerEnable : PlayerActionBase
    {
        #region Constructors

        public PlayerEnable(Selector selector) : base("playerEnable", selector, "False") { }

        #endregion

        #region Functions

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player)
        {
            if (player && player.RuntimePlayer)
                player.SetCustomActive(modifier.GetBool(Index(0), true, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            modifierCard.BoolGenerator(modifier, reference, "Enabled", Index(0));
        }

        #endregion
    }
}
