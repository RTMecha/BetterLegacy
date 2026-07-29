using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetCurrentKey : ModifierVariableBase
    {
        #region Constructors

        public GetCurrentKey() => SetupModifier("KEYCODE_VAR");

        #endregion

        #region Values

        public override string Name => "getCurrentKey";

        public override ModifierCategoryType Category => ModifierCategoryType.Controls;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => ProjectArrhythmia.Input.GetKeyCodeDown().ToString();

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
        }

        #endregion
    }
}
