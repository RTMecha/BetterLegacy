using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class RandomCompare : ModifierTriggerBase
    {
        #region Constructors

        public RandomCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "random" + comparison.ToString();
            SetupModifier("0", "0", "1");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Main;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!modifier.HasResult())
            {
                if (modifierLoop.reference is PAObjectBase obj)
                    modifier.Result = comparison.Compare(RandomHelper.FromIDRange(RandomHelper.CurrentSeed, obj.id, modifier.GetInt(1, 0, modifierLoop.variables), modifier.GetInt(2, 0, modifierLoop.variables)), modifier.GetInt(0, 0, modifierLoop.variables));
                else
                    modifier.Result = comparison.Compare(UnityRandom.Range(modifier.GetInt(1, 0, modifierLoop.variables), modifier.GetInt(2, 0, modifierLoop.variables)), modifier.GetInt(0, 0, modifierLoop.variables));
            }

            return modifier.HasResult() && modifier.GetResult<bool>();
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop) => modifier.Result = default;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Minimum", 1, 0);
            modifierCard.IntegerGenerator(modifier, reference, "Maximum", 2, 0);
            modifierCard.IntegerGenerator(modifier, reference, "Compare To", 0, 0);
        }

        #endregion
    }
}
