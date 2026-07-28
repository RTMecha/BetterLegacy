using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetQuickElement : ModifierVariableBase
    {
        #region Constructors

        public GetQuickElement() => SetupModifier("QE_VAR", "atan_idle", "0");

        #endregion

        #region Values

        public override string Name => "getQuickElement";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (QuickElementManager.inst.quickElements.TryGetValue(modifier.GetValue(1, modifierLoop.variables), out QuickElement quickElement))
                return QuickElementManager.inst.Interpolate(quickElement, modifier.GetFloat(2, 0f, modifierLoop.variables));
            return null;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Quick Element Name", 1);
            modifierCard.SingleGenerator(modifier, reference, "Time", 2);
        }

        #endregion
    }
}
