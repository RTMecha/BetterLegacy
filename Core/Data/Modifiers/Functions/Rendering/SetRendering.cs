using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetRendering : ModifierActionBase
    {
        #region Constructors

        public SetRendering() => SetupModifier("False", "0", "0", "1", "0");

        #endregion

        #region Values

        public override string Name => "setRendering";

        public override CategoryType Category => CategoryType.Rendering;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject || !solidObject.gameObject)
                return;

            var doubleSided = modifier.GetBool(0, false, modifierLoop.variables);
            var gradientType = modifier.GetInt(1, 0, modifierLoop.variables);
            var colorBlendMode = modifier.GetInt(2, 0, modifierLoop.variables);
            var gradientScale = modifier.GetFloat(3, 1f, modifierLoop.variables);
            var gradientRotation = modifier.GetFloat(4, 0f, modifierLoop.variables);

            if (modifier.constant)
            {
                var cache = modifier.GetResultOrDefault(() =>
                {
                    var cache = new Cache();
                    cache.UpdateCache(doubleSided, gradientType, colorBlendMode, gradientScale, gradientRotation);
                    cache.Apply(solidObject);
                    DestroyModifierResult.Init(solidObject.gameObject, modifier);
                    return cache;
                });

                if (!cache.Is(doubleSided, gradientType, colorBlendMode, gradientScale, gradientRotation))
                {
                    cache.UpdateCache(doubleSided, gradientType, colorBlendMode, gradientScale, gradientRotation);
                    cache.Apply(solidObject);
                }
            }
            else
            {
                solidObject.UpdateRendering(
                    gradientType: gradientType,
                    renderType: solidObject.gameObject.layer switch
                    {
                        RTLevel.FOREGROUND_LAYER => (int)RenderLayerType.Foreground,
                        RTLevel.BACKGROUND_LAYER => (int)RenderLayerType.Background,
                        RTLevel.UI_LAYER => (int)RenderLayerType.UI,
                        _ => 0,
                    },
                    doubleSided: doubleSided,
                    gradientScale: gradientScale,
                    gradientRotation: gradientRotation,
                    colorBlendMode: colorBlendMode);
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.BoolGenerator(modifier, reference, "Double Sided", 0);
            modifierCard.DropdownGenerator(modifier, reference, "Gradient Type", 1, CoreHelper.ToOptionData<GradientType>());
            modifierCard.SingleGenerator(modifier, reference, "Gradient Scale", 3, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Gradient Rotation", 4, 0, 15f, 3f);
            modifierCard.DropdownGenerator(modifier, reference, "Color Blend Mode", 2, CoreHelper.ToOptionData<ColorBlendMode>());
        }

        #endregion

        #region Sub Classes

        public class Cache
        {
            public bool doubleSided;
            public int gradientType;
            public int colorBlendMode;
            public float gradientScale = 1f;
            public float gradientRotation;

            public void UpdateCache(bool doubleSided, int gradientType, int colorBlendMode, float gradientScale, float gradientRotation)
            {
                this.doubleSided = doubleSided;
                this.gradientType = gradientType;
                this.colorBlendMode = colorBlendMode;
                this.gradientScale = gradientScale;
                this.gradientRotation = gradientRotation;
            }

            public bool Is(bool doubleSided, int gradientType, int colorBlendMode, float gradientScale, float gradientRotation) =>
                this.doubleSided == doubleSided &&
                this.gradientType == gradientType &&
                this.colorBlendMode == colorBlendMode &&
                this.gradientScale == gradientScale &&
                this.gradientRotation == gradientRotation;

            public void Apply(SolidObject solidObject)
            {
                solidObject.UpdateRendering(
                    gradientType: gradientType,
                    renderType: solidObject.gameObject.layer switch
                    {
                        RTLevel.FOREGROUND_LAYER => (int)RenderLayerType.Foreground,
                        RTLevel.BACKGROUND_LAYER => (int)RenderLayerType.Background,
                        RTLevel.UI_LAYER => (int)RenderLayerType.UI,
                        _ => 0,
                    },
                    doubleSided: doubleSided,
                    gradientScale: gradientScale,
                    gradientRotation: gradientRotation,
                    colorBlendMode: colorBlendMode);
            }
        }

        #endregion
    }
}
