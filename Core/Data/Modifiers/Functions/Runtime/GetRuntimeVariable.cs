using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetRuntimeVariable : ModifierVariableBase
    {
        #region Constructors

        public GetRuntimeVariable() => SetupModifier("RUNTIME_VAR", "RUNTIME_KEY");

        #endregion

        #region Values

        public override string Name => "getRuntimeVariable";

        public override ModifierCategoryType Category => ModifierCategoryType.Runtime;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            var key = FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables);
            return !string.IsNullOrEmpty(key) && RTLevel.Current.variables.TryGetValue(key, out string value) ? value : null;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Runtime Key", 1);
        }

        #endregion
    }
}
