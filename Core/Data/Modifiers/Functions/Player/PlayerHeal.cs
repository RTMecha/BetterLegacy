using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerHeal : PlayerActionBase
    {
        #region Constructors

        public PlayerHeal(Selector selector) : base("playerHeal", selector, "1") { }

        #endregion

        #region Functions

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player)
        {
            if (!RTBeatmap.Current.Invincible && !modifier.constant)
                player?.RuntimePlayer?.Heal(RTMath.Clamp(modifier.GetInt(Index(0), 1, modifierLoop.variables), 0, int.MaxValue));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            modifierCard.IntegerGenerator(modifier, reference, "Heal Amount", Index(0));
        }

        #endregion
    }
}
