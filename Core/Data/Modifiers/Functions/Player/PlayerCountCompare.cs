using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerCountCompare : ModifierTriggerBase
    {
        #region Constructors

        public PlayerCountCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "playerCount" + comparison.ToString();
            SetupModifier("1");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => base.Compatibility;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => comparison.Compare(PlayerManager.inst.players.Count, modifier.GetInt(0, 0, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Compare To", 0, 0);
        }

        #endregion
    }
}
