using System.Collections.Generic;
using System.Linq;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ClearSpawnedPrefabs : ModifierActionBase
    {
        #region Constructors

        public ClearSpawnedPrefabs()
        {
            SetupModifier(false, "Object Group");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "clearSpawnedPrefabs";

        public override ModifierCategoryType Category => ModifierCategoryType.Prefab;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IPrefabable prefabable)
                return;

            var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(0, modifierLoop.variables)).ToList();

            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = modifierLoop.reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : modifierLoop.reference.GetParentRuntime();
                var toPool = new List<PrefabObject>();
                var toDestroy = new List<PrefabObject>();

                foreach (var modifyable in modifyables)
                {
                    for (int i = 0; i < modifyable.Modifiers.Count; i++)
                    {
                        var otherModifier = modifyable.Modifiers[i];

                        if (otherModifier.TryGetResult(out PrefabObject prefabObjectResult))
                        {
                            Collect(prefabObjectResult, toPool, toDestroy);
                            otherModifier.Result = null;
                            continue;
                        }

                        if (!otherModifier.TryGetResult(out List<PrefabObject> result))
                            continue;

                        for (int k = 0; k < result.Count; k++)
                            Collect(result[k], toPool, toDestroy);

                        result.Clear();
                        otherModifier.Result = null;
                    }
                }

                if (runtimeLevel != null && !toPool.IsEmpty())
                    runtimeLevel.SleepPrefabs(toPool);
                if (!toDestroy.IsEmpty())
                {
                    var ids = new HashSet<string>(toDestroy.Select(x => x.id));
                    for (int i = 0; i < toDestroy.Count; i++)
                        runtimeLevel?.UpdatePrefab(toDestroy[i], false);
                    GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && ids.Contains(x.id));
                }
            });
        }
        static void Collect(PrefabObject prefabObject, List<PrefabObject> toPool, List<PrefabObject> toDestroy)
        {
            if (!prefabObject)
                return;
            var runtimeObject = prefabObject.runtimeObject;
            if (runtimeObject && runtimeObject.poolable && CoreConfig.Instance != null && CoreConfig.Instance.PrefabPooling.Value)
                toPool.Add(prefabObject);
            else
                toDestroy.Add(prefabObject);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
        }

        #endregion
    }
}
