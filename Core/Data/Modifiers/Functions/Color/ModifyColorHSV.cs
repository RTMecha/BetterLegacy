using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ModifyColorHSV : ModifierActionBase
    {
        #region Constructors

        public ModifyColorHSV(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "modifyColorHSV";
            if (isGroup)
                Name += "Other";
            SetupModifier("0", "0", "0", "0", "0", "0");
            if (isGroup)
                Modifier.values.Add("Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Color;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.FullBeatmapCompatible : ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject();

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var h1 = modifier.GetFloat(0, 0f, modifierLoop.variables);
            var s1 = modifier.GetFloat(1, 0f, modifierLoop.variables);
            var v1 = modifier.GetFloat(2, 0f, modifierLoop.variables);
            var h2 = modifier.GetFloat(3, 0f, modifierLoop.variables);
            var s2 = modifier.GetFloat(4, 0f, modifierLoop.variables);
            var v2 = modifier.GetFloat(5, 0f, modifierLoop.variables);

            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(6, modifierLoop.variables)));
                if (list.IsEmpty())
                    return;

                // queue post tick so the color overrides the sequence color
                RTLevel.Current.postTick.Enqueue(() =>
                {
                    foreach (var bm in list)
                    {
                        if (!bm.runtimeObject || !bm.runtimeObject.visualObject.gameObject)
                            return;

                        if (bm.runtimeObject.visualObject.isGradient && bm.runtimeObject.visualObject is SolidObject solidObject)
                        {
                            var colors = solidObject.GetColors();
                            solidObject.SetColor(
                                RTColors.ChangeColorHSV(colors.startColor, h1, s1, v1),
                                RTColors.ChangeColorHSV(colors.endColor, h2, s2, v2));
                        }
                        else
                            bm.runtimeObject.visualObject.SetColor(RTColors.ChangeColorHSV(bm.runtimeObject.visualObject.GetPrimaryColor(), h1, s1, v1));
                    }
                });
                return;
            }

            if (modifierLoop.reference is BackgroundObject backgroundObject)
            {
                backgroundObject.runtimeObject.SetColor(
                    RTColors.ChangeColorHSV(backgroundObject.runtimeObject.mainColor, h1, s1, v1),
                    RTColors.ChangeColorHSV(backgroundObject.runtimeObject.fadeColor, h2, s2, v2));
                return;
            }

            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                if (runtimeObject.visualObject.isGradient && runtimeObject.visualObject is SolidObject solidObject)
                {
                    var colors = solidObject.GetColors();
                    solidObject.SetColor(
                        RTColors.ChangeColorHSV(colors.startColor, h1, s1, v1),
                        RTColors.ChangeColorHSV(colors.endColor, h2, s2, v2));
                }
                else
                    runtimeObject.visualObject.SetColor(RTColors.ChangeColorHSV(runtimeObject.visualObject.GetPrimaryColor(), h1, s1, v1));
            });
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Start Hue", 0, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Start Sat", 1, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Start Value", 2, 0f);
            modifierCard.SingleGenerator(modifier, reference, "End Hue", 3, 0f);
            modifierCard.SingleGenerator(modifier, reference, "End Sat", 4, 0f);
            modifierCard.SingleGenerator(modifier, reference, "End Value", 5, 0f);
            if (isGroup)
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 6);
        }

        #endregion
    }
}
