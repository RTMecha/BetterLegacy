using BetterLegacy.Arcade.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ShowTitleCard : ModifierActionBase
    {
        #region Constructors

        public ShowTitleCard() => SetupModifier(false, string.Empty, string.Empty, string.Empty);

        #endregion

        #region Values

        public override string Name => "showTitleCard";

        public override ModifierCategoryType Category => ModifierCategoryType.Level;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant)
                return;

            RTGameManager.inst.ShowTitleCard(
                title: modifier.GetValue(0),
                artist: modifier.GetValue(1),
                creator: modifier.GetValue(2));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Song Title", 0);
            modifierCard.StringGenerator(modifier, reference, "Artist Name", 1);
            modifierCard.StringGenerator(modifier, reference, "Creator Name", 2);
        }

        #endregion
    }
}
