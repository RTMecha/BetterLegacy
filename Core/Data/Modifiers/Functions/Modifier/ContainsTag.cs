using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ContainsTag : ModifierTriggerBase
    {
        #region Constructors

        public ContainsTag() => SetupModifier("Tag");

        #endregion

        #region Values

        public override string Name => "containsTag";

        public override ModifierCategoryType Category => ModifierCategoryType.Modifier;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => modifierLoop.reference is IPrefabable prefabable && prefabable.FromPrefab && prefabable.TryGetPrefabObject(out PrefabObject prefabObject) &&
                prefabObject.Tags.Contains(modifier.GetValue(0, modifierLoop.variables)) || modifierLoop.reference is IModifyable modifyable && modifyable.Tags.Contains(modifier.GetValue(0, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Tag", 0);
        }

        #endregion
    }
}
