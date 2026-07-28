using UnityEngine;

using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetPlayerVelocity : PlayerActionBase
    {
        public SetPlayerVelocity(int axis, Selector selector) : base("setPlayerVelocity" + (axis == 1 ? "Y" : axis == 0 ? "X" : string.Empty, axis == -1 ? new string[] { "0", "10" } : new string[] { "10" }), selector) => this.axis = axis;

        readonly int axis;

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player)
        {
            if (player.RuntimePlayer && player.RuntimePlayer.rb)
                player.RuntimePlayer.rb.velocity = axis switch
                {
                    0 => new Vector2(modifier.GetFloat(Index(0), 0f, modifierLoop.variables), player.RuntimePlayer.rb.velocity.y),
                    1 => new Vector2(player.RuntimePlayer.rb.velocity.x, modifier.GetFloat(Index(0), 0f, modifierLoop.variables)),
                    _ => new Vector2(modifier.GetFloat(Index(0), 0f, modifierLoop.variables), modifier.GetFloat(Index(1), 0f, modifierLoop.variables)),
                };
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            modifierCard.SingleGenerator(modifier, reference, axis != -1 ? "Value" : "X", Index(0), 0f);
            if (axis != -1)
                modifierCard.SingleGenerator(modifier, reference, "Y", Index(1), 0f);
        }
    }
}
