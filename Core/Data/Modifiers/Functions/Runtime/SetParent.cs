using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetParent : ModifierActionBase
    {
        #region Constructors

        public SetParent(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "setParent";
            if (isGroup)
                Name += "Other";
            SetupModifier(false, "Object Group");
            if (isGroup)
            {
                Modifier.values.Add("False");
                Modifier.values.Add(string.Empty);
            }
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Runtime;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible.WithPrefabObject(true);

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant)
                return;

            if (!isGroup)
            {
                if (modifierLoop.reference is not IParentable child)
                    return;

                var prefabable = modifierLoop.reference.AsPrefabable();
                if (prefabable == null)
                    return;

                var group = modifier.GetValue(0, modifierLoop.variables);

                var result = modifier.GetResultOrDefault(() => ParentableGroupCache.GetSingle(modifier, prefabable, group));

                if (result.tag != group)
                {
                    result = ParentableGroupCache.GetSingle(modifier, prefabable, group);
                    modifier.Result = result;
                }

                if (group == string.Empty)
                    ModifiersHelper.SetParent(child, string.Empty);
                else if (result.obj && child.CanParent(result.obj))
                    ModifiersHelper.SetParent(child, result.obj);
                else
                    CoreHelper.LogError($"CANNOT PARENT OBJECT!\nID: {child.ID}");
            }
            else
            {
                var prefabable = modifierLoop.reference.AsPrefabable();
                if (prefabable == null)
                    return;

                var group = modifier.GetValue(2, modifierLoop.variables);

                var result = modifier.GetResultOrDefault(() => ParentableGroupCache.GetGroup(modifier, prefabable, group, modifier.GetValue(0, modifierLoop.variables)));

                if (result.tag != group)
                {
                    result = ParentableGroupCache.GetGroup(modifier, prefabable, group, modifier.GetValue(0, modifierLoop.variables));
                    modifier.Result = result;
                }

                var isEmpty = modifier.GetBool(1, false, modifierLoop.variables);

                bool failed = false;
                foreach (var parentable in result.group)
                {
                    if (isEmpty)
                        ModifiersHelper.SetParent(parentable, string.Empty);
                    else if (parentable.CanParent(result.obj))
                        ModifiersHelper.SetParent(parentable, result.obj);
                    else
                        failed = true;
                }

                if (failed)
                    CoreHelper.LogError($"CANNOT PARENT OBJECT {modifierLoop.reference}");
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);

            if (isGroup)
            {
                modifierCard.BoolGenerator(modifier, reference, "Clear Parent", 1, false);
                modifierCard.GroupFieldGenerator(modifier, reference, "Parent Group To", 2);
            }
        }

        #endregion

        #region Sub Classes

        public class ParentableGroupCache : GenericGroupCache<IParentable, BeatmapObject>
        {
            public ParentableGroupCache() { }

            public string otherGroup;
            bool multi;

            public static ParentableGroupCache GetSingle(Modifier modifier, IPrefabable prefabable, string group)
            {
                var cache = new ParentableGroupCache();
                cache.tag = group;
                cache.UpdateCache(modifier, prefabable, group);
                return cache;
            }

            public static ParentableGroupCache GetGroup(Modifier modifier, IPrefabable prefabable, string group, string otherGroup)
            {
                var cache = new ParentableGroupCache();
                cache.tag = group;
                cache.otherGroup = otherGroup;
                cache.multi = true;
                cache.UpdateCache(modifier, prefabable, group);
                return cache;
            }

            public override void UpdateCache(Modifier modifier, IPrefabable prefabable, string tag)
            {
                this.tag = tag;
                if (!multi)
                {
                    if (!string.IsNullOrEmpty(tag) && GameData.Current.TryFindObjectWithTag(modifier, prefabable, tag, out BeatmapObject target))
                        obj = target;
                }
                else
                {
                    if (!string.IsNullOrEmpty(tag) && GameData.Current.TryFindObjectWithTag(modifier, prefabable, tag, out BeatmapObject target))
                        obj = target;
                    if (!obj && prefabable is BeatmapObject parent)
                        obj = parent;
                    group = GameData.Current.FindParentablesWithTag(modifier, prefabable, otherGroup);
                }
            }
        }

        #endregion
    }
}
