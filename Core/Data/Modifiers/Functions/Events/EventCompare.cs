using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class EventCompare : ModifierTriggerBase
    {
        #region Constructors

        public EventCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "event" + comparison.ToString();
            SetupModifier("0", "0", "0", "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Events;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => RTLevel.Current && RTLevel.Current.eventEngine &&
            comparison.Compare(
                a: RTLevel.Current.eventEngine.Interpolate(modifier.GetInt(1, 0, modifierLoop.variables), modifier.GetInt(2, 0, modifierLoop.variables), modifier.GetFloat(0, RTLevel.Current.FixedTime, modifierLoop.variables)),
                b: modifier.GetFloat(3, 0f, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(EventLibrary.displayNames));
            modifierCard.IntegerGenerator(modifier, reference, "Value Index", 2, 0);
            modifierCard.SingleGenerator(modifier, reference, "Time", 0, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Equals", 3, 0f);
        }

        #endregion
    }
}
