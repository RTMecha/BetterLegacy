using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LocalVariableCompare : ModifierTriggerBase
    {
        #region Constructors

        public LocalVariableCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "localVariable" + comparison.ToString();
            SetupModifier("0", "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Modifier;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => modifierLoop.variables.TryGetValue(modifier.GetValue(0), out string result) &&
                (comparison == NumberComparison.Equals ?
                result == modifier.GetValue(1, modifierLoop.variables) :
                comparison.Compare((float.TryParse(result, out float num) ? num : Parser.TryParse(result, 0)), modifier.GetFloat(1, 0f, modifierLoop.variables)));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            if (comparison == NumberComparison.Equals)
                modifierCard.StringGenerator(modifier, reference, "Compare To", 1);
            else
                modifierCard.SingleGenerator(modifier, reference, "Compare To", 1, 0);
        }

        #endregion
    }
}
