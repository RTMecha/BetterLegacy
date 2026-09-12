using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerRespawn : PlayerActionBase
    {
        #region Constructors

        public PlayerRespawn(Selector selector) : base("playerRespawn", selector) { }

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant)
                return;

            if (selector == Selector.All)
            {
                PlayerManager.inst.RespawnPlayers();
                return;
            }
            base.Run(modifier, modifierLoop);
        }

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player) => PlayerManager.inst.RespawnPlayer(player);

        #endregion
    }
}
