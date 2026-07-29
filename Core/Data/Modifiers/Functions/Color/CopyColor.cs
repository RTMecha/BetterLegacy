using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class CopyColor : ModifierActionBase
    {
        #region Constructors

        public CopyColor(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "copyColor";
            if (isGroup)
                Name += "Other";
            SetupModifier("Object Group", "True", "True");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Color;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject)
                return;

            var applyColor1 = modifier.GetBool(1, true, modifierLoop.variables);
            var applyColor2 = modifier.GetBool(2, true, modifierLoop.variables);

            if (isGroup)
            {
                var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(0, modifierLoop.variables)));

                if (list.IsEmpty())
                    return;

                var runtimeObject = beatmapObject.runtimeObject;
                if (!runtimeObject)
                    return;

                // queue post tick so the color overrides the sequence color
                RTLevel.Current.postTick.Enqueue(() =>
                {
                    foreach (var bm in list)
                    {
                        var otherRuntimeObject = bm.runtimeObject;
                        if (!otherRuntimeObject)
                            continue;

                        ModifiersHelper.CopyColor(otherRuntimeObject, runtimeObject, applyColor1, applyColor2);
                    }
                });
                return;
            }

            var other = modifier.GetResultOrDefault(() => GameData.Current.FindObjectWithTag(modifier, beatmapObject, modifier.GetValue(0, modifierLoop.variables)));

            if (!other || !other.runtimeObject)
                return;

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() => ModifiersHelper.CopyColor(beatmapObject.runtimeObject, other.runtimeObject, applyColor1, applyColor2));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
            modifierCard.BoolGenerator(modifier, reference, "Apply Color 1", 1, true);
            modifierCard.BoolGenerator(modifier, reference, "Apply Color 2", 2, true);
        }

        #endregion
    }
}
