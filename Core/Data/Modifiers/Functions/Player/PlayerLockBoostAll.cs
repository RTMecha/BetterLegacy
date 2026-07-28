using BetterLegacy.Core.Components.Player;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerLockBoostAll : ModifierActionBase
    {
        #region Constructors

        public PlayerLockBoostAll() => SetupModifier("True", "", "");

        #endregion

        #region Values

        public override string Name => "playerLockBoostAll";

        public override CategoryType Category => CategoryType.Player;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        // this modifier only exists for compatibility.
        public override bool DisplayInEditor => false;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.values.Count > 3 && !string.IsNullOrEmpty(modifier.GetValue(1)) && bool.TryParse(modifier.GetValue(0, modifierLoop.variables), out bool lockBoost))
                RTPlayer.LockBoost = lockBoost;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
