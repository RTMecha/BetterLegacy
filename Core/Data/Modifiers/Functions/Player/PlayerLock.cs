using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerLock : PlayerActionBase
    {
        #region Constructors

        public PlayerLock(int axis, Selector selector) : base("playerLock" + (axis == 1 ? "Y" : "X"), selector, "True") => this.axis = axis;

        #endregion

        #region Values

        readonly int axis;

        #endregion

        #region Functions

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player) => player?.RuntimePlayer?.LockMovement(axis, modifier.GetBool(Index(0), true, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            modifierCard.BoolGenerator(modifier, reference, "Lock " + (axis == 1 ? "Y" : "X"), Index(0));
        }

        #endregion
    }
}
