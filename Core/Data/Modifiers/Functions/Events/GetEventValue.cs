using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetEventValue : ModifierActionBase
    {
        #region Constructors

        public GetEventValue() => SetupModifier("EVENT_VAR", "0", "0", "0", "1", "0", "-99999", "99999", "99999");

        #endregion

        #region Values

        public override string Name => "getEventValue";

        public override CategoryType Category => CategoryType.Events;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!RTLevel.Current.eventEngine)
                return;

            float multiply = modifier.GetFloat(4, 0f, modifierLoop.variables);
            float offset = modifier.GetFloat(5, 0f, modifierLoop.variables);
            float min = modifier.GetFloat(6, -9999f, modifierLoop.variables);
            float max = modifier.GetFloat(7, 9999f, modifierLoop.variables);
            float loop = modifier.GetFloat(8, 9999f, modifierLoop.variables);

            var value = RTLevel.Current.eventEngine.Interpolate(modifier.GetInt(1, 0, modifierLoop.variables), modifier.GetInt(2, 0, modifierLoop.variables), RTLevel.Current.CurrentTime - modifier.GetFloat(3, 0f, modifierLoop.variables));

            value = RTMath.Clamp((value - offset) * multiply % loop, min, max);

            modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = value.ToString();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

            modifierCard.DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData(EventLibrary.displayNames));
            modifierCard.IntegerGenerator(modifier, reference, "Axis", 2, 0);

            modifierCard.SingleGenerator(modifier, reference, "Delay", 3, 0f);

            modifierCard.SingleGenerator(modifier, reference, "Multiply", 4, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Offset", 5, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Min", 6, -99999f);
            modifierCard.SingleGenerator(modifier, reference, "Max", 7, 99999f);
            modifierCard.SingleGenerator(modifier, reference, "Loop", 8, 99999f);
        }

        #endregion
    }
}
