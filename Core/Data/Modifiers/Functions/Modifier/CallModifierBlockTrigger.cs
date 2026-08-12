using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class CallModifierBlockTrigger : ModifierTriggerBase
    {
        #region Constructors

        public CallModifierBlockTrigger() => SetupModifier("modifierBlock");

        #endregion

        #region Values

        public override string Name => "callModifierBlockTrigger";

        public override ModifierCategoryType Category => ModifierCategoryType.Modifier;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var name = modifier.GetValue(0, modifierLoop.variables);
            var prefabable = modifierLoop.reference.AsPrefabable();
            var prefab = prefabable?.GetPrefab();
            var cache = modifier.GetResultOrDefault(() =>
            {
                var prefabable = modifierLoop.reference.AsPrefabable();
                var prefab = prefabable?.GetPrefab();

                var cache = new Cache();
                cache.modifierLoop = new ModifierLoop(modifierLoop.reference, modifierLoop.variables);
                if (prefabable != null && prefab && prefab.modifierBlocks.TryFind(x => x.Name == name, out ModifierBlock prefabModifierBlock))
                    cache.modifierBlock = prefabModifierBlock.Copy(false);
                else if (GameData.Current.modifierBlocks.TryFind(x => x.Name == name, out ModifierBlock modifierBlock))
                    cache.modifierBlock = modifierBlock.Copy(false);
                return cache;
            });
            return cache && cache.modifierBlock && cache.modifierLoop && cache.modifierBlock.Run(cache.modifierLoop).result;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Function Name", 0);
        }

        #endregion

        #region Sub Classes

        public class Cache : Exists
        {
            public ModifierLoop modifierLoop;
            public ModifierBlock modifierBlock;
        }

        #endregion
    }
}
