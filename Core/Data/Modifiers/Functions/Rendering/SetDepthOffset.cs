using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetDepthOffset : ModifierActionBase
    {
        #region Constructors

        public SetDepthOffset() => SetupModifier("0", "False");

        #endregion

        #region Values

        public override string Name => "setDepthOffset";

        public override ModifierCategoryType Category => ModifierCategoryType.Rendering;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BackgroundObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is BackgroundObject backgroundObject)
                backgroundObject.runtimeObject?.SetDepthOffset(modifier.GetBool(1, false, modifierLoop.variables) ? -(modifier.GetInt(0, 0, modifierLoop.variables) - (backgroundObject.iterations - 1)) : modifier.GetInt(0, 0, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Depth Offset", 0, max: int.MaxValue);
            modifierCard.BoolGenerator(modifier, reference, "Inverse", 1);
        }

        #endregion
    }
}
