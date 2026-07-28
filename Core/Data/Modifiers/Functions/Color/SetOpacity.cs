using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetOpacity : ModifierActionBase
    {
        #region Constructors

        public SetOpacity(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "setOpacity";
            if (isGroup)
                Name += "Other";
            SetupModifier("0.5");
            if (isGroup)
                Modifier.values.Add("Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Color;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.FullBeatmapCompatible : ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var opacity = modifier.GetFloat(0, 1f, modifierLoop.variables);

            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, modifierLoop.variables)));
                if (list.IsEmpty())
                    return;

                // queue post tick so the color overrides the sequence color
                RTLevel.Current.postTick.Enqueue(() =>
                {
                    foreach (var bm in list)
                    {
                        if (bm.runtimeObject && bm.runtimeObject.visualObject)
                            bm.runtimeObject.visualObject.SetColor(RTColors.FadeColor(bm.runtimeObject.visualObject.GetPrimaryColor(), opacity));
                    }
                });
                return;
            }

            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() => runtimeObject.visualObject.SetColor(RTColors.FadeColor(runtimeObject.visualObject.GetPrimaryColor(), opacity)));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 1);
            }
            modifierCard.SingleGenerator(modifier, reference, "Amount", 0, 1f);
        }

        #endregion
    }
}
