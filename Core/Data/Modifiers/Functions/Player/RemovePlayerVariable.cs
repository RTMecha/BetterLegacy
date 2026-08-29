using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class RemovePlayerVariable : PlayerActionBase
    {
        #region Constructors

        public RemovePlayerVariable(Selector selector) : base("removePlayerVariable", selector, "name") { }

        #endregion

        #region Functions

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player) => player.RemoveVariable(modifier.GetValue(Index(0), modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            modifierCard.StringGenerator(modifier, reference, "Name", Index(0));
        }

        #endregion
    }
}
