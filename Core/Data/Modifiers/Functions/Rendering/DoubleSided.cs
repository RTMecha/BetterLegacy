using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class DoubleSided : ModifierActionBase
    {
        #region Constructors

        public DoubleSided() => SetupModifier(false);

        #endregion

        #region Values

        public override string Name => "doubleSided";

        public override ModifierCategoryType Category => ModifierCategoryType.Rendering;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject is SolidObject solidObject && solidObject.gameObject)
                solidObject.UpdateRendering((int)beatmapObject.gradientType, (int)beatmapObject.renderLayerType, true, beatmapObject.gradientScale, beatmapObject.gradientRotation, (int)beatmapObject.colorBlendMode);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
