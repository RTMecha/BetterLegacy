using System.Collections.Generic;
using System.Linq;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class CallModifiersTrigger : ModifierTriggerBase
    {
        #region Constructors

        public CallModifiersTrigger()
        {
            SetupModifier("Object Group");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "callModifiersTrigger";

        public override CategoryType Category => CategoryType.Modifier;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var prefabable = modifierLoop.reference.AsPrefabable();
            if (prefabable == null)
                return false;

            var tag = modifier.GetValue(0, modifierLoop.variables);
            if (string.IsNullOrEmpty(tag) || !GameData.Current.TryFindModifyableWithTag(modifier, prefabable, tag, out IModifyable modifyable) || modifyable.Modifiers.IsEmpty())
                return false;

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
            return cache.modifierBlock.Run(cache.Item2).result;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
        }

        #endregion
    }
}
