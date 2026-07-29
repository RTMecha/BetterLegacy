using System.Collections.Generic;
using System.Linq;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class CallModifiers : ModifierActionBase
    {
        #region Constructors

        public CallModifiers()
        {
            SetupModifier("Object Group");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "callModifiers";

        public override ModifierCategoryType Category => ModifierCategoryType.Modifier;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var prefabable = modifierLoop.reference.AsPrefabable();
            if (prefabable == null)
                return;

            var tag = modifier.GetValue(0, modifierLoop.variables);
            if (string.IsNullOrEmpty(tag) || !GameData.Current.TryFindModifyableWithTag(modifier, prefabable, tag, out IModifyable modifyable) || modifyable.Modifiers.IsEmpty())
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var modifierBlock = new ModifierBlock(modifierLoop.reference.ReferenceType)
                {
                    Modifiers = new List<Modifier>(modifyable.Modifiers.Select(x => x.Copy(false))),
                    OrderModifiers = modifyable.OrderModifiers,
                    Tags = modifyable.Tags,
                };
                // prevent recursion.
                if (modifierBlock.Modifiers.TryFind(x => x.id == modifier.id, out Modifier otherModifier))
                    otherModifier.enabled = false;
                return (modifierBlock, new ModifierLoop(modifierLoop.reference, modifierLoop.variables));
            });
            cache.modifierBlock.Run(cache.Item2);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
        }

        #endregion
    }
}
