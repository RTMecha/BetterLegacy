using System.Collections.Generic;
using System.Linq;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ClearSpawnedPrefabs : ModifierActionBase
    {
        #region Constructors

        public ClearSpawnedPrefabs()
        {
            IsGroup = true;
            SetupModifier(false, "Object Group");
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

                foreach (var modifyable in modifyables)
                {
                    for (int i = 0; i < modifyable.Modifiers.Count; i++)
                    {
                        var otherModifier = modifyable.Modifiers[i];

                        if (otherModifier.TryGetResult(out PrefabObject prefabObjectResult))
                        {
                            runtimeLevel?.UpdatePrefab(prefabObjectResult, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObjectResult.id);

                            otherModifier.Result = null;
                            continue;
                        }

                        if (!otherModifier.TryGetResult(out List<PrefabObject> result))
                            continue;

                        for (int k = 0; k < result.Count; k++)
                        {
                            var prefabObject = result[k];

                            runtimeLevel?.UpdatePrefab(prefabObject, false);
                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);
                        }

                        result.Clear();
                        otherModifier.Result = null;
                    }
                }
            });
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
        }

        #endregion
    }
}
