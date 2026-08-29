using BetterLegacy.Core.Data.Player;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ClearPlayerVariables : PlayerActionBase
    {
        #region Constructors

        public ClearPlayerVariables(Selector selector) : base("clearPlayerVariables", selector) { }

        #endregion

        #region Functions

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player) => player.ClearVariables();

        #endregion
    }
}
