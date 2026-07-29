using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ForLoopPlayer : ForLoop
    {
        #region Constructors

        public ForLoopPlayer() => SetupModifier("INDEX_VAR", "1");

        #endregion

        #region Values

        public override string Name => "forLoopPlayer";

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override int GetStartIndex(Modifier modifier, ModifierLoop modifierLoop) => 0;

        public override int GetEndCount(Modifier modifier, ModifierLoop modifierLoop) => PlayerManager.Players.Count;

        public override int GetIncrement(Modifier modifier, ModifierLoop modifierLoop) => modifier.GetInt(1, 1, modifierLoop.variables);

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.IntegerGenerator(modifier, reference, "Increment", 1, 1);
        }

        #endregion
    }
}
