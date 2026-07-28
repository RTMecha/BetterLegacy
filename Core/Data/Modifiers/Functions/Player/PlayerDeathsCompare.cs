using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerDeathsCompare : ModifierTriggerBase
    {
        #region Constructors

        public PlayerDeathsCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "playerDeaths" + comparison.ToString();
            SetupModifier("1");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Player;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => comparison.Compare(RTBeatmap.Current.deaths.Count, modifier.GetInt(0, 0, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Compare To", 0, 0);
        }

        #endregion
    }
}
