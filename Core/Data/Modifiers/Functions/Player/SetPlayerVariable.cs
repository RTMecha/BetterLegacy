using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetPlayerVariable : PlayerActionBase
    {
        #region Constructors

        public SetPlayerVariable(Selector selector) : base("setPlayerVariable", selector, "name", "value") { }

        #endregion

        #region Functions

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player) => player.SetVariable(modifier.GetValue(Index(0), modifierLoop.variables), modifier.GetValue(Index(1), modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            modifierCard.StringGenerator(modifier, reference, "Name", Index(0));
            modifierCard.StringGenerator(modifier, reference, "Value", Index(1));
        }

        #endregion
    }
}
