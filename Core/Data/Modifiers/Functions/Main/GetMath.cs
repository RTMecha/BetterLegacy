using System.Collections.Generic;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetMath : ModifierVariableBase
    {
        #region Constructors

        public GetMath() => SetupModifier("MATH_VAR", "1 + 1");

        #endregion

        #region Values

        public override string Name => "getMath";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IEvaluatable evaluatable)
            {
                var numberVariables = new Dictionary<string, float>();
                ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);

                return RTMath.Parse(FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables), RTLevel.Current?.evaluationContext, numberVariables).ToString();
            }

            try
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);

                return RTMath.Parse(FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables), RTLevel.Current?.evaluationContext, numberVariables, evaluatable.GetObjectFunctions()).ToString();
            }
            catch
            {
                return null;
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Value", 1);
        }

        #endregion
    }
}
