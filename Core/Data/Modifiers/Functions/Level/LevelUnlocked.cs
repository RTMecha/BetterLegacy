using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LevelUnlocked : ModifierTriggerBase
    {
        #region Constructors

        public LevelUnlocked() => SetupModifier("0");

        #endregion

        #region Values

        public override string Name => "levelUnlocked";

        public override ModifierCategoryType Category => ModifierCategoryType.Level;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var id = modifier.GetValue(0, modifierLoop.variables);
            return LevelManager.Levels.TryFind(x => x.id == id, out Level.Level level) && !level.Locked;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "ID", 0);
        }

        #endregion
    }
}
