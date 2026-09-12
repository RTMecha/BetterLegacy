using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetPlayerVariable : ModifierVariableBase
    {
        #region Constructors

        public GetPlayerVariable() => SetupModifier($"PLAYER_VAR", "0", "varName");

        #endregion

        #region Values

        public override string Name => "getPlayerVariable";

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!PlayerManager.inst.players.TryGetAt(modifier.GetInt(1, 0, modifierLoop.variables), out PAPlayer player))
                return null;
            var variables = player.GetPlayerVariables();
            return variables != null && variables.TryGetValue(modifier.GetValue(2, modifierLoop.variables), out string value) ? value : null;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.IntegerGenerator(modifier, reference, "Player Index", 1, 0, max: int.MaxValue);
            modifierCard.StringGenerator(modifier, reference, "Player Var Name", 2);
        }

        #endregion
    }
}
