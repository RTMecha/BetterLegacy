using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class MathCompare : ModifierTriggerBase
    {
        #region Constructors

        public MathCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "math" + comparison.ToString();
            SetupModifier("0", "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IEvaluatable evaluatable)
                return false;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);
            var functions = evaluatable.GetObjectFunctions();

            return comparison.Compare(RTMath.Parse(modifier.GetValue(0, modifierLoop.variables), RTLevel.Current?.evaluationContext, numberVariables, functions), RTMath.Parse(modifier.GetValue(1, modifierLoop.variables), RTLevel.Current?.evaluationContext, numberVariables, functions));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "First", 0);
            modifierCard.StringGenerator(modifier, reference, "Second", 1);
        }

        #endregion
    }
}
