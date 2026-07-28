using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetSeed : ModifierActionBase
    {
        #region Constructors

        public SetSeed() => SetupModifier(false, string.Empty);

        #endregion

        #region Values

        public override string Name => "setSeed";

        public override CategoryType Category => CategoryType.Runtime;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!modifier.constant)
                RTLevel.Current?.InitSeed(modifier.GetValue(0, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Seed", 0);
        }

        #endregion
    }
}
