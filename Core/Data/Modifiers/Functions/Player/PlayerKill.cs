using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerKill : PlayerActionBase
    {
        #region Constructors

        public PlayerKill(Selector selector) : base("playerKill", selector) { }

        #endregion

        #region Functions

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player)
        {
            if (!RTBeatmap.Current.Invincible && !modifier.constant)
                player?.RuntimePlayer?.Kill();
        }

        #endregion
    }
}
