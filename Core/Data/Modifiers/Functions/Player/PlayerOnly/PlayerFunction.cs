using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerFunction : ModifierActionBase
    {
        #region Constructors

        public PlayerFunction(Type type)
        {
            this.type = type;
            Name = type.ToString().ToLower();
            Modifier = type switch
            {
                Type.Kill => CreateModifier(Name, false),
                Type.Hit => CreateModifier(Name, false, "0"),
                Type.Boost => CreateModifier(Name, false, string.Empty, string.Empty),
                Type.Shoot => CreateModifier(Name, false),
                Type.Pulse => CreateModifier(Name, false),
                Type.Jump => CreateModifier(Name, false),
                _ => null,
            };
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.PAPlayerCompatible;

        readonly Type type;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not PAPlayer player)
                return;
            switch (type)
            {
                case Type.Kill: {
                        player.Health = 0;
                        break;
                    }
                case Type.Hit: {
                        var damage = modifier.GetInt(0, 0, modifierLoop.variables);
                        if (damage <= 1)
                            player.RuntimePlayer?.Hit();
                        else
                            player.RuntimePlayer?.Hit(damage);
                        break;
                    }
                case Type.Boost: {
                        var xStr = modifier.GetValue(0, modifierLoop.variables);
                        var yStr = modifier.GetValue(1, modifierLoop.variables);

                        if (!string.IsNullOrEmpty(xStr))
                            player.RuntimePlayer.lastMoveHorizontal = Parser.TryParse(xStr, 0f);

                        if (!string.IsNullOrEmpty(yStr))
                            player.RuntimePlayer.lastMoveVertical = Parser.TryParse(yStr, 0f);

                        player.RuntimePlayer?.Boost();
                        break;
                    }
                case Type.Shoot: {
                        player.RuntimePlayer?.Shoot();
                        break;
                    }
                case Type.Pulse: {
                        player.RuntimePlayer?.Pulse();
                        break;
                    }
                case Type.Jump: {
                        player.RuntimePlayer?.Jump();
                        break;
                    }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            switch (type)
            {
                case Type.Hit: {
                        modifierCard.IntegerGenerator(modifier, reference, "Hit Amount", 0);
                        break;
                    }
                case Type.Boost: {
                        modifierCard.SingleGenerator(modifier, reference, "X", 0);
                        modifierCard.SingleGenerator(modifier, reference, "Y", 1);
                        break;
                    }
            }
        }

        #endregion

        #region Sub Classes

        public enum Type
        {
            Kill,
            Hit,
            Boost,
            Shoot,
            Pulse,
            Jump,
        }

        #endregion
    }
}
