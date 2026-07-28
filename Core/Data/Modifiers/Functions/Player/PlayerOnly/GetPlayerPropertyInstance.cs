using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetPlayerPropertyInstance : ModifierActionBase
    {
        #region Constructors

        public GetPlayerPropertyInstance(Property property)
        {
            this.property = property;
            Name = "get" + property.ToString();
            Modifier = property switch
            {
                Property.Health => CreateModifier(Name, "HEALTH_VAR"),
                Property.Lives => CreateModifier(Name, "LIVES_VAR"),
                Property.MaxHealth => CreateModifier(Name, "MAX_HEALTH_VAR"),
                Property.MaxLives => CreateModifier(Name, "MAX_LIVES_VAR"),
                Property.Index => CreateModifier(Name, "INDEX_VAR"),
                Property.Move => CreateModifier(Name, "MOVE_X_VAR", "MOVE_Y_VAR"),
                Property.MoveX => CreateModifier(Name, "MOVE_X_VAR"),
                Property.MoveY => CreateModifier(Name, "MOVE_Y_VAR"),
                Property.Look => CreateModifier(Name, "LOOK_X_VAR", "LOOK_Y_VAR"),
                Property.LookX => CreateModifier(Name, "LOOK_X_VAR"),
                Property.LookY => CreateModifier(Name, "LOOK_Y_VAR"),
                _ => null,
            };
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullPlayerCompatible;

        readonly Property property;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var player = modifierLoop.reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : modifierLoop.reference as PAPlayer;
            if (!player)
                return;

            switch (property)
            {
                case Property.Health: {
                        modifierLoop.variables[modifier.GetValue(0)] = player.Health.ToString();
                        break;
                    }
                case Property.Lives: {
                        modifierLoop.variables[modifier.GetValue(0)] = player.lives.ToString();
                        break;
                    }
                case Property.MaxHealth: {
                        modifierLoop.variables[modifier.GetValue(0)] = player.GetMaxHealth().ToString();
                        break;
                    }
                case Property.MaxLives: {
                        modifierLoop.variables[modifier.GetValue(0)] = player.GetMaxLives().ToString();
                        break;
                    }
                case Property.Index: {
                        modifierLoop.variables[modifier.GetValue(0)] = player.index.ToString();
                        break;
                    }
                case Property.Move: {
                        var move = player.Input.Move.Vector;
                        if (move.magnitude > 1f && modifier.GetBool(2, true, modifierLoop.variables))
                            move = move.normalized;
                        modifierLoop.variables[modifier.GetValue(0)] = move.x.ToString();
                        modifierLoop.variables[modifier.GetValue(1)] = move.y.ToString();
                        break;
                    }
                case Property.MoveX: {
                        var move = player.Input.Move.Vector;
                        if (move.magnitude > 1f && modifier.GetBool(1, true, modifierLoop.variables))
                            move = move.normalized;
                        modifierLoop.variables[modifier.GetValue(0)] = move.x.ToString();
                        break;
                    }
                case Property.MoveY: {
                        var move = player.Input.Move.Vector;
                        if (move.magnitude > 1f && modifier.GetBool(1, true, modifierLoop.variables))
                            move = move.normalized;
                        modifierLoop.variables[modifier.GetValue(0)] = move.y.ToString();
                        break;
                    }
                case Property.Look: {
                        var move = player.Input.Look.Vector;
                        if (move.magnitude > 1f && modifier.GetBool(2, true, modifierLoop.variables))
                            move = move.normalized;
                        modifierLoop.variables[modifier.GetValue(0)] = move.x.ToString();
                        modifierLoop.variables[modifier.GetValue(1)] = move.y.ToString();
                        break;
                    }
                case Property.LookX: {
                        var move = player.Input.Look.Vector;
                        if (move.magnitude > 1f && modifier.GetBool(1, true, modifierLoop.variables))
                            move = move.normalized;
                        modifierLoop.variables[modifier.GetValue(0)] = move.x.ToString();
                        break;
                    }
                case Property.LookY: {
                        var move = player.Input.Look.Vector;
                        if (move.magnitude > 1f && modifier.GetBool(1, true, modifierLoop.variables))
                            move = move.normalized;
                        modifierLoop.variables[modifier.GetValue(0)] = move.y.ToString();
                        break;
                    }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            switch (property)
            {
                case Property.Move: {
                        modifierCard.StringGenerator(modifier, reference, "X Variable Name", 0, renderVariables: false);
                        modifierCard.StringGenerator(modifier, reference, "Y Variable Name", 1, renderVariables: false);
                        modifierCard.BoolGenerator(modifier, reference, "Normalize", 2);
                        break;
                    }
                case Property.MoveX: {
                        modifierCard.StringGenerator(modifier, reference, "X Variable Name", 0, renderVariables: false);
                        modifierCard.BoolGenerator(modifier, reference, "Normalize", 1);
                        break;
                    }
                case Property.MoveY: {
                        modifierCard.StringGenerator(modifier, reference, "Y Variable Name", 0, renderVariables: false);
                        modifierCard.BoolGenerator(modifier, reference, "Normalize", 1);
                        break;
                    }
                case Property.Look: {
                        modifierCard.StringGenerator(modifier, reference, "X Variable Name", 0, renderVariables: false);
                        modifierCard.StringGenerator(modifier, reference, "Y Variable Name", 1, renderVariables: false);
                        modifierCard.BoolGenerator(modifier, reference, "Normalize", 2);
                        break;
                    }
                case Property.LookX: {
                        modifierCard.StringGenerator(modifier, reference, "X Variable Name", 0, renderVariables: false);
                        modifierCard.BoolGenerator(modifier, reference, "Normalize", 1);
                        break;
                    }
                case Property.LookY: {
                        modifierCard.StringGenerator(modifier, reference, "Y Variable Name", 0, renderVariables: false);
                        modifierCard.BoolGenerator(modifier, reference, "Normalize", 1);
                        break;
                    }
                default: {
                        modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
                        break;
                    }
            }
        }

        #endregion

        #region Sub Classes

        public enum Property
        {
            Health,
            Lives,
            MaxHealth,
            MaxLives,
            Index,
            Move,
            MoveX,
            MoveY,
            Look,
            LookX,
            LookY,
        }

        #endregion
    }
}
