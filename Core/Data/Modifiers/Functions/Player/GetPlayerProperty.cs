using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetPlayerProperty : ModifierActionBase
    {
        #region Constructors

        public GetPlayerProperty(Property property)
        {
            this.property = property;
            Name = "getPlayer" + property.ToString();
            SetupModifier($"PLAYER_{property.ToString().ToUpper()}_VAR", "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        readonly Property property;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, modifierLoop.variables), out PAPlayer player))
                return;
            var value = property switch
            {
                Property.Health => player.Health.ToString(),
                Property.Lives => player.lives.ToString(),
                Property.PosX => player.RuntimePlayer?.rb?.transform?.position.x.ToString(),
                Property.PosY => player.RuntimePlayer?.rb?.transform?.position.y.ToString(),
                Property.Rot => player.RuntimePlayer?.rb?.transform?.eulerAngles.z.ToString(),
                _ => string.Empty,
            }; ;
            if (!string.IsNullOrEmpty(value))
                modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = value;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: int.MaxValue);
        }

        #endregion

        #region Sub Classes

        public enum Property
        {
            Health,
            Lives,
            PosX,
            PosY,
            Rot,
        }

        #endregion
    }
}
