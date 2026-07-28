using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerEnableProperty : PlayerActionBase
    {
        #region Constructors

        public PlayerEnableProperty(Property property, Selector selector) : base("playerEnable" + property.ToString(), selector, property == Property.Move ? new string[] { "True", "True" } : new string[] { "True" }) => this.property = property;

        #endregion

        #region Values

        readonly Property property;

        #endregion

        #region Functions

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player)
        {
            if (!player || !player.RuntimePlayer)
                return;

            switch (property)
            {
                case Property.Move: {
                        player.RuntimePlayer.CanMove = modifier.GetBool(Index(0), true, modifierLoop.variables);
                        player.RuntimePlayer.CanRotate = modifier.GetBool(Index(1), true, modifierLoop.variables);
                        break;
                    }
                case Property.Boost: {
                        player.RuntimePlayer.CanBoost = modifier.GetBool(Index(0), true, modifierLoop.variables);
                        break;
                    }
                case Property.Damage: {
                        player.RuntimePlayer.canTakeDamageModified = modifier.GetBool(Index(0), true, modifierLoop.variables);
                        break;
                    }
                case Property.Jump: {
                        player.RuntimePlayer.allowJumping = modifier.GetBool(Index(0), true, modifierLoop.variables);
                        break;
                    }
                case Property.ReversedJump: {
                        player.RuntimePlayer.allowReversedJumping = modifier.GetBool(Index(0), true, modifierLoop.variables);
                        break;
                    }
                case Property.WallJump: {
                        player.RuntimePlayer.allowWallJumping = modifier.GetBool(Index(0), true, modifierLoop.variables);
                        break;
                    }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            if (property == Property.Move)
            {
                modifierCard.BoolGenerator(modifier, reference, "Can Move", Index(0), true);
                modifierCard.BoolGenerator(modifier, reference, "Can Rotate", Index(1), true);
                return;
            }
            modifierCard.BoolGenerator(modifier, reference, "Enabled", Index(0));
        }

        #endregion

        #region Sub Classes

        public enum Property
        {
            Move,
            Boost,
            Damage,
            Jump,
            ReversedJump,
            WallJump,
        }

        #endregion
    }
}
