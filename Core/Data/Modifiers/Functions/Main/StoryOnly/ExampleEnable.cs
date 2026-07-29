using BetterLegacy.Companion.Entity;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ExampleEnable : ModifierActionBase
    {
        #region Constructors

        public ExampleEnable() => SetupModifier(false, "False");

        #endregion

        #region Values

        public override string Name => "exampleEnableDEVONLY";

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.StoryOnlyCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop) => Example.Current?.model?.SetActive(modifier.GetBool(0, false, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.BoolGenerator(modifier, reference, "Active", 0, false);
        }

        #endregion
    }
}
