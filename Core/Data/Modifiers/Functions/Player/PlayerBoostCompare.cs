using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerBoostCompare : ModifierTriggerBase
    {
        #region Constructors

        public PlayerBoostCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "playerBoost" + comparison.ToString();
            SetupModifier("0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => base.Compatibility;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => comparison.Compare(RTBeatmap.Current.boosts.Count, modifier.GetInt(0, 0, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Compare To", 0, 0);
        }

        #endregion
    }
}
