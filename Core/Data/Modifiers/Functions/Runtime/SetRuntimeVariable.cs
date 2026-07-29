using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetRuntimeVariable : ModifierActionBase
    {
        #region Constructors

        public SetRuntimeVariable() => SetupModifier("RUNTIME_KEY", "Value");

        #endregion

        #region Values

        public override string Name => "setRuntimeVariable";

        public override ModifierCategoryType Category => ModifierCategoryType.Runtime;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var key = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
            if (!string.IsNullOrEmpty(key))
                RTLevel.Current.variables[key] = modifier.GetValue(1, modifierLoop.variables);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Runtime Key", 0);
            modifierCard.StringGenerator(modifier, reference, "Runtime Val", 1);
        }

        #endregion 
    }
}
