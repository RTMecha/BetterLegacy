using BetterLegacy.Companion.Entity;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ExampleSay : ModifierActionBase
    {
        #region Constructors

        public ExampleSay() => SetupModifier(false, "Something!");

        #endregion

        #region Values

        public override string Name => "exampleSayDEVONLY";

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.StoryOnlyCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop) => Example.Current?.chatBubble?.Say(modifier.GetValue(0, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Dialogue", 0);
        }

        #endregion
    }
}
