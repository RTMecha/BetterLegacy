using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class UpdatePrefab : ModifierActionBase
    {
        #region Constructors

        public UpdatePrefab() => SetupModifier(false, "True");

        #endregion

        #region Values

        public override string Name => "updatePrefab";

        public override CategoryType Category => CategoryType.Prefab;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullBeatmapCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant)
                return;

            var prefabable = modifierLoop.reference.AsPrefabable();
            if (prefabable == null || !prefabable.FromPrefab)
                return;

            var reinsert = modifier.GetBool(0, true, modifierLoop.variables);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var prefabObject = prefabable.GetPrefabObject();
                if (prefabObject)
                    prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, reinsert: reinsert);
            });
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.BoolGenerator(modifier, reference, "Respawn", 0);
        }

        #endregion
    }
}
