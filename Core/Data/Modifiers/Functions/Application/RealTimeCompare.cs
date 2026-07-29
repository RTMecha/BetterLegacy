using System;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class RealTimeCompare : ModifierTriggerBase
    {
        #region Constructors

        public RealTimeCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "realTime" + comparison.ToString();
            if (comparison == NumberComparison.Equals)
                SetupModifier("yyyy-MM-dd_HH.mm.ss", "2019-06-15_00.00.00");
            else
                SetupModifier("1", "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Application;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (comparison == NumberComparison.Equals)
                return DateTime.Now.ToString(modifier.GetValue(0, modifierLoop.variables)) == modifier.GetValue(1, modifierLoop.variables);

            var dateTime = DateTime.Now;

            var type = modifier.GetInt(0, 0, modifierLoop.variables);
            var dateValue = type switch
            {
                0 => dateTime.Millisecond,
                1 => dateTime.Second,
                2 => dateTime.Minute,
                3 => dateTime.Hour % 12,
                4 => dateTime.Hour,
                5 => dateTime.Day,
                6 => dateTime.Month,
                7 => dateTime.Year,
                8 => (long)dateTime.DayOfWeek,
                9 => dateTime.DayOfYear,
                10 => dateTime.Ticks,
                _ => 0,
            };
            return comparison.Compare(dateValue, modifier.GetInt(1, 0, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (comparison == NumberComparison.Equals)
            {
                modifierCard.StringGenerator(modifier, reference, "Format", 0);
                modifierCard.StringGenerator(modifier, reference, "Equals", 1);
                return;
            }

            modifierCard.DropdownGenerator(modifier, reference, "Type", 0, CoreHelper.StringToOptionData("Millisecond", "Second", "Minute", "12 Hour", "24 Hour", "Day", "Month", "Year", "Day of Week", "Day of Year", "Ticks"));
            modifierCard.IntegerGenerator(modifier, reference, "Compare To", 1);
        }

        #endregion
    }
}
