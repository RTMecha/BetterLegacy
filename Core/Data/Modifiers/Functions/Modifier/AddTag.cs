using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class AddTag : ModifierActionBase
    {
        #region Constructors

        public AddTag()
        {
            SetupModifier("Object Group", "Tag");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "addTag";

        public override ModifierCategoryType Category => ModifierCategoryType.Modifier;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.PrefabObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var prefabable = modifierLoop.reference.AsPrefabable();
            if (prefabable == null)
                return;

            var group = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(0, modifierLoop.variables));
            var tag = modifier.GetValue(1, modifierLoop.variables);
            foreach (var obj in group)
            {
                if (obj is IPrefabable p && p.FromPrefab && !obj.Tags.Contains(tag))
                    obj.Tags.Add(tag);
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.StringGenerator(modifier, reference, "Object Group", 0);
            modifierCard.StringGenerator(modifier, reference, "Tag", 1);
        }

        #endregion
    }
}
