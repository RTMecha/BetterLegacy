using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class CallModifierBlock : ModifierActionBase
    {
        #region Constructors

        public CallModifierBlock() => SetupModifier("modifierBlock");

        #endregion

        #region Values

        public override string Name => "callModifierBlock";

        public override ModifierCategoryType Category => ModifierCategoryType.Modifier;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var name = modifier.GetValue(0, modifierLoop.variables);
            if (string.IsNullOrEmpty(name))
                return;

            var modifierBlockLoop = modifier.GetResultOrDefault(() => new ModifierLoop(modifierLoop.reference, modifierLoop.variables));

            var prefabable = modifierLoop.reference.AsPrefabable();
            var prefab = prefabable?.GetPrefab();
            if (prefabable != null && prefab && prefab.modifierBlocks.TryFind(x => x.Name == name, out ModifierBlock prefabModifierBlock))
                prefabModifierBlock.Run(modifierBlockLoop);
            else if (GameData.Current.modifierBlocks.TryFind(x => x.Name == name, out ModifierBlock modifierBlock))
                modifierBlock.Run(modifierBlockLoop);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Function Name", 0);
        }

        #endregion
    }
}
