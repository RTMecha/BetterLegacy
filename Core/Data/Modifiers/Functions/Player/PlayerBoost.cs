using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerBoost : PlayerActionBase
    {
        #region Constructors

        public PlayerBoost(Selector selector) : base("playerBoost", selector, "", "") { }

        #endregion

        #region Functions

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player)
        {
            if (!player.RuntimePlayer)
                return;

            var xStr = modifier.GetValue(0, modifierLoop.variables);
            var yStr = modifier.GetValue(1, modifierLoop.variables);

            if (!string.IsNullOrEmpty(xStr))
                player.RuntimePlayer.lastMoveHorizontal = Parser.TryParse(xStr, 0f);

            if (!string.IsNullOrEmpty(yStr))
                player.RuntimePlayer.lastMoveVertical = Parser.TryParse(yStr, 0f);

            player.RuntimePlayer.Boost();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            modifierCard.SingleGenerator(modifier, reference, "X", Index(0));
            modifierCard.SingleGenerator(modifier, reference, "Y", Index(1));
        }

        #endregion
    }
}
