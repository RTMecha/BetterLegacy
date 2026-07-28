using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetPlayerProperty : PlayerActionBase
    {
        #region Constructors

        public SetPlayerProperty(Property property, Selector selector) : base("setPlayer" + property.ToString(), selector, "1") => this.property = property;

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
                case Property.MoveSpeed: {
                        player.RuntimePlayer.modifiedIdleSpeed = modifier.GetFloat(Index(0), 1f, modifierLoop.variables);
                        break;
                    }
                case Property.BoostSpeed: {
                        player.RuntimePlayer.modifiedBoostSpeed = modifier.GetFloat(Index(0), 1f, modifierLoop.variables);
                        break;
                    }
                case Property.JumpIntensity: {
                        player.RuntimePlayer.modifiedJumpIntensity = modifier.GetFloat(Index(0), 1f, modifierLoop.variables);
                        break;
                    }
                case Property.JumpGravity: {
                        player.RuntimePlayer.modifiedJumpGravity = modifier.GetFloat(Index(0), 1f, modifierLoop.variables);
                        break;
                    }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            modifierCard.SingleGenerator(modifier, reference, "Value", Index(0), 1f);
        }

        #endregion

        #region Sub Classes

        public enum Property
        {
            MoveSpeed,
            BoostSpeed,
            JumpIntensity,
            JumpGravity,
        }

        #endregion
    }
}
