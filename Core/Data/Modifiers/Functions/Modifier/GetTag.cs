using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetTag : ModifierVariableBase
    {
        #region Constructors

        public GetTag() => SetupModifier("TAG_VAR", "0");

        #endregion

        #region Values

        public override string Name => "getTag";

        public override ModifierCategoryType Category => ModifierCategoryType.Modifier;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => modifierLoop.reference is IModifyable modifyable && modifyable.Tags.TryGetAt(modifier.GetInt(1, 0, modifierLoop.variables), out string tag) ? tag : null;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.IntegerGenerator(modifier, reference, "Index", 1, 0);
        }

        #endregion
    }
}
