using BetterLegacy.Core.Data.Player;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerCancelBoost : PlayerActionBase
    {
        #region Constructors

        public PlayerCancelBoost(Selector selector) : base("playerCancelBoost", selector) { }

        #endregion

        #region Functions

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player) => player?.RuntimePlayer?.StopBoosting();

        #endregion
    }
}
