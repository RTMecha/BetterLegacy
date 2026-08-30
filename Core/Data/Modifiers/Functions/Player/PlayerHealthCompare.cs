using System.Linq;

using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerHealthCompare : ModifierTriggerBase
    {
        #region Constructors

        public PlayerHealthCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "playerHealth" + comparison.ToString();
            SetupModifier("3");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Player;

        public override ModifierCompatibility Compatibility => base.Compatibility;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var health = modifier.GetInt(0, 0, modifierLoop.variables);
            return !PlayerManager.inst.players.IsEmpty() && PlayerManager.inst.players.Any(x => comparison.Compare(x.health, health));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Compare To", 0, 0);
        }

        #endregion
    }
}
