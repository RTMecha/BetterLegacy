using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class UpdateObject : ModifierActionBase
    {
        #region Constructors

        public UpdateObject(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "updateObject";
            if (isGroup)
                Name += "Other";
            SetupModifier(false, "True", "False");
            if (isGroup)
                Modifier.values.Insert(0, "Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Runtime;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullBeatmapCompatible;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant)
                return;

            var reinsert = modifier.GetBool(isGroup ? 1 : 0, true, modifierLoop.variables);
            var retainRuntimeModifiers = modifier.GetBool(isGroup ? 2 : 1, false, modifierLoop.variables);
            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var prefabables = GameData.Current.FindPrefabablesWithTag(modifier, prefabable, modifier.GetValue(0));
                if (prefabables.IsEmpty())
                    return;

                RTLevel.Current.postTick.Enqueue(() =>
                {
                    foreach (var other in prefabables)
                    {
                        if (other is BeatmapObject beatmapObject)
                        {
                            var parentRuntime = beatmapObject.GetParentRuntime();
                            parentRuntime?.UpdateObject(beatmapObject, reinsert: reinsert, updateModifiers: false);

                            parentRuntime?.RemoveModifiers(beatmapObject);
                            if (reinsert || retainRuntimeModifiers)
                                parentRuntime?.AddModifiers(beatmapObject);
                        }
                        if (other is BackgroundObject backgroundObject)
                        {
                            var parentRuntime = backgroundObject.GetParentRuntime();
                            parentRuntime?.UpdateBackgroundObject(backgroundObject, reinsert: reinsert, updateModifiers: false);

                            parentRuntime?.RemoveModifiers(backgroundObject);
                            if (reinsert || retainRuntimeModifiers)
                                parentRuntime?.AddModifiers(backgroundObject);
                        }
                        if (other is PrefabObject prefabObject)
                        {
                            var parentRuntime = prefabObject.GetParentRuntime();
                            parentRuntime?.UpdatePrefab(prefabObject, reinsert: reinsert, updateModifiers: false);

                            parentRuntime?.RemoveModifiers(prefabObject);
                            if (reinsert || retainRuntimeModifiers)
                                parentRuntime?.AddModifiers(prefabObject.GetPrefab(), prefabObject);
                        }
                    }
                });
                return;
            }
            RTLevel.Current.postTick.Enqueue(() =>
            {
                if (modifierLoop.reference is BeatmapObject beatmapObject)
                {
                    var parentRuntime = beatmapObject.GetParentRuntime();
                    parentRuntime?.UpdateObject(beatmapObject, reinsert: reinsert, updateModifiers: false);

                    parentRuntime?.RemoveModifiers(beatmapObject);
                    if (reinsert || retainRuntimeModifiers)
                        parentRuntime?.AddModifiers(beatmapObject);
                }
                if (modifierLoop.reference is BackgroundObject backgroundObject)
                {
                    var parentRuntime = backgroundObject.GetParentRuntime();
                    parentRuntime?.UpdateBackgroundObject(backgroundObject, reinsert: reinsert, updateModifiers: false);

                    parentRuntime?.RemoveModifiers(backgroundObject);
                    if (reinsert || retainRuntimeModifiers)
                        parentRuntime?.AddModifiers(backgroundObject);
                }
                if (modifierLoop.reference is PrefabObject prefabObject)
                {
                    var parentRuntime = prefabObject.GetParentRuntime();
                    parentRuntime?.UpdatePrefab(prefabObject, reinsert: reinsert, updateModifiers: false);

                    parentRuntime?.RemoveModifiers(prefabObject);
                    if (reinsert || retainRuntimeModifiers)
                        parentRuntime?.AddModifiers(prefabObject.GetPrefab(), prefabObject);
                }
            });
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
            }
            modifierCard.BoolGenerator(modifier, reference, "Respawn", isGroup ? 1 : 0);
            modifierCard.BoolGenerator(modifier, reference, "Retain Modifiers", isGroup ? 2 : 1);
        }

        #endregion
    }
}
