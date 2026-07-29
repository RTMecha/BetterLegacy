using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetComparison : ModifierVariableBase
    {
        #region Constructors

        public GetComparison(bool isMath)
        {
            this.isMath = isMath;
            Name = "getComparison";
            if (isMath)
                Name += "Math";
            Modifier = isMath ? CreateModifier(Name, "COMPAREMATH_VAR", "1 + 1", "1 + 1", "0") : CreateModifier(Name, "COMPARE_VAR", "text equals", "text equals");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        readonly bool isMath;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isMath)
            {
                if (modifierLoop.reference is not IEvaluatable evaluatable)
                    return null;

                try
                {
                    var numberVariables = evaluatable.GetObjectVariables();
                    var functions = evaluatable.GetObjectFunctions();
                    var comparison = Parser.TryParse(modifier.GetValue(3, modifierLoop.variables), NumberComparison.Equals);
                    var a = RTMath.Parse(FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables), RTLevel.Current?.evaluationContext, numberVariables, functions);
                    var b = RTMath.Parse(FormatStringVariables(modifier.GetValue(2, modifierLoop.variables), modifierLoop.variables), RTLevel.Current?.evaluationContext, numberVariables, functions);
                    return comparison.Compare(a, b).ToString();
                }
                catch
                {
                    return null;
                }
            }

            return (FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables) == FormatStringVariables(modifier.GetValue(2, modifierLoop.variables), modifierLoop.variables)).ToString();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Compare From", 1);
            modifierCard.StringGenerator(modifier, reference, "Compare To", 2);
            if (isMath)
                modifierCard.DropdownGenerator(modifier, reference, "Comparison", 3, CoreHelper.ToOptionData<NumberComparison>());
        }

        #endregion
    }
}
