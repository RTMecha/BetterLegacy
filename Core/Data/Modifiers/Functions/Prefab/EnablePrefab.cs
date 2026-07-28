using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class EnablePrefab : ModifierActionBase
    {
        #region Constructors

        public EnablePrefab() => SetupModifier("True");

        #endregion

        #region Values

        public override string Name => "enablePrefab";

        public override CategoryType Category => CategoryType.Prefab;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullBeatmapCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var prefabable = modifierLoop.reference.AsPrefabable();
            if (prefabable != null && prefabable.FromPrefab)
                prefabable.GetPrefabObject()?.runtimeObject?.SetCustomActive(modifier.GetBool(0, true, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.BoolGenerator(modifier, reference, "Enabled", 0);
        }

        #endregion
    }
}
