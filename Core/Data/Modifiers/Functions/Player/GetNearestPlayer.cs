using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetNearestPlayer : ModifierVariableBase
    {
        #region Constructors

        public GetNearestPlayer() => SetupModifier("PLAYER_INDEX_VAR");

        #endregion

        #region Values

        public override string Name => "getNearestPlayer";

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is ITransformable transformable)
                return PlayerManager.GetClosestPlayerIndex(transformable.GetFullPosition()).ToString();
            return null;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
        }

        #endregion
    }
}
