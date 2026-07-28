using System;

using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetEasing : ModifierActionBase
    {
        #region Constructors

        public GetEasing(bool isName)
        {
            this.isName = isName;
            Name = isName ? "getEasingName" : "getEasing";
            SetupModifier("EASING_VAR", "0");
        }

        #endregion

        #region Values

        readonly bool isName;

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Animation;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var value = modifier.GetValue(1, modifierLoop.variables);
            if (Enum.TryParse(value, out Easing easing))
                modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = isName ? easing.ToString() : ((int)easing).ToString();
            else if (isName && int.TryParse(value, out int num))
                modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = ((Easing)num).ToString();
            else
                modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = value;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.EaseGenerator(modifier, reference, 1);
        }

        #endregion
    }
}
