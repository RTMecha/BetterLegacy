using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetGameMode : ModifierActionBase
    {
        #region Constructors

        public SetGameMode() => SetupModifier("0");

        #endregion

        #region Values

        public override string Name => "setGameMode";

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop) => RTPlayer.GameMode = Parser.TryParse(modifier.GetValue(0, modifierLoop.variables), true, GameMode.Regular);

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.DropdownGenerator(modifier, reference, "Set Game Mode", 0, CoreHelper.StringToOptionData("Regular", "Platformer"));
        }

        #endregion
    }
}
