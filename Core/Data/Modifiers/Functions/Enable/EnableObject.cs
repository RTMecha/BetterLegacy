using System.Collections.Generic;
using System.Linq;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class EnableObject : ModifierActionBase
    {
        #region Constructors

        public EnableObject(bool isTree, bool isGroup)
        {
            this.isTree = isTree;
            this.isGroup = isGroup;
            Name = "enableObject";
            if (isTree)
                Name += "Tree";
            if (isGroup)
                Name += "Other";
            SetupModifier("True", "False");
            if (isGroup)
                Modifier.values.Add("Object Group");
            if (isTree)
                Modifier.values.Add("True");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Enable;

        public override ModifierCompatibility Compatibility => !isTree && !isGroup ? ModifierCompatibility.FullBeatmapCompatible.WithPAPlayer() : isGroup ? ModifierCompatibility.FullBeatmapCompatible : ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isTree;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void ValidateModifier(Modifier modifier, IModifyable modifyable)
        {
            if (modifier.version != 0)
                return;

            switch (modifier.Name)
            {
                case "enableObjectTree": {
                        if (modifier.values.Count > 2)
                        {
                            modifier.values.Move(2, 0); // move enabled to 0
                            modifier.values.Move(2, 1); // move reset to 1
                        }
                        // 0: enabled
                        // 1: reset
                        // 2: use self
                        // ----
                        // 0: use self
                        // 1: reset
                        // 2: enabled
                        break;
                    }
                case "enableObjectOther": {
                        if (modifier.values.Count > 2)
                        {
                            modifier.values.Move(2, 0); // move enabled to 0
                            modifier.values.Move(2, 1); // move reset to 1
                        }
                        // 0: enabled
                        // 1: reset
                        // 2: object group
                        // ----
                        // 0: object group
                        // 1: reset
                        // 2: enabled
                        break;
                    }
                case "enableObjectTreeOther": {
                        if (modifier.values.Count > 3)
                        {
                            modifier.values.Move(0, 3); // move use self to 3
                            modifier.values.Move(2, 0); // move enabled to 0
                            modifier.values.Move(1, 2); // move object group to 2
                        }
                        // 0: enabled
                        // 1: reset
                        // 2: object group
                        // 3: use self
                        // ----
                        // orig:
                        // 1: object group
                        // 2: reset
                        // 3: enabled
                        // 0: use self
                        break;
                    }
            }

            if (modifier.values.IsEmpty())
                return;

            var value = modifier.GetValue(0);
            if (value == "0")
                value = "True";
            modifier.SetValue(0, value);
            modifier.version++;
        }

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var enabled = modifier.GetBool(0, true, modifierLoop.variables);

            if (!isGroup)
            {
                if (isTree)
                {
                    var useSelf = modifier.GetBool(2, true, modifierLoop.variables);
                    var list = modifier.GetResultOrDefault(() =>
                    {
                        if (modifierLoop.reference is not BeatmapObject beatmapObject)
                            return new List<BeatmapObject>();

                        var root = useSelf ? beatmapObject : beatmapObject.GetParentChain().Last();
                        return root.GetChildTree();
                    });

                    for (int i = 0; i < list.Count; i++)
                        list[i].runtimeObject?.SetCustomActive(enabled);
                    return;
                }

                if (modifierLoop.reference is ICustomActivatable activatable)
                {
                    activatable.SetCustomActive(enabled);
                    return;
                }

                if (modifierLoop.reference is IPrefabable prefabable)
                    ModifiersHelper.SetObjectActive(prefabable, enabled);
                return;
            }

            if (isTree)
            {
                var list = modifier.GetResultOrDefault(() =>
                {
                    var resultList = new List<BeatmapObject>();

                    var prefabable = modifierLoop.reference.AsPrefabable();
                    if (prefabable == null)
                        return resultList;

                    var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(2, modifierLoop.variables));
                    var useSelf = modifier.GetBool(3, true, modifierLoop.variables);

                    foreach (var bm in beatmapObjects)
                    {
                        var beatmapObject = useSelf ? bm : bm.GetParentChain().Last();
                        resultList.AddRange(beatmapObject.GetChildTree());
                    }
                    return resultList;
                });
                for (int i = 0; i < list.Count; i++)
                    list[i].runtimeObject?.SetCustomActive(enabled);
            }
            else
            {
                var cache = modifier.GetResultOrDefault(() =>
                {
                    var prefabable = modifierLoop.reference.AsPrefabable();
                    if (prefabable == null)
                        return null;
                    var tag = modifier.GetValue(2, modifierLoop.variables);
                    return new GenericGroupCache<IPrefabable>(tag, GameData.Current.FindPrefabablesWithTag(modifier, prefabable, tag));
                });
                if (cache == null || cache.group.IsEmpty())
                    return;

                foreach (var other in cache.group)
                    ModifiersHelper.SetObjectActive(other, enabled);
            }
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!modifier.GetBool(1, false, modifierLoop.variables))
            {
                modifier.Result = default;
                return;
            }

            if (!isTree && !isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                if (prefabable.GetRuntimeObject() is ICustomActivatable activatable)
                    activatable.SetCustomActive(false);
            }
            if (!isTree && isGroup)
            {
                if (!modifier.GetBool(1, false, modifierLoop.variables))
                {
                    modifier.Result = default;
                    return;
                }

                if (modifier.TryGetResult(out GenericGroupCache<IPrefabable> cache) && cache.group != null && !cache.group.IsEmpty())
                    foreach (var other in cache.group)
                        ModifiersHelper.SetObjectActive(other, false);

                modifier.Result = default;
            }
            if (isTree)
            {
                if (!modifier.TryGetResult(out List<BeatmapObject> list))
                    return;

                for (int i = 0; i < list.Count; i++)
                    list[i].runtimeObject?.SetCustomActive(false);

                modifier.Result = default;
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.BoolGenerator(modifier, reference, "Enabled", 0, true);
            modifierCard.BoolGenerator(modifier, reference, "Reset", 1, true);
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 2);
            }
            if (isTree)
                modifierCard.BoolGenerator(modifier, reference, "Use Self", isGroup ? 3 : 2);
        }

        #endregion
    }
}
