using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ObjectActiveOther : ModifierTriggerBase
    {
        #region Constructors

        public ObjectActiveOther()
        {
            SetupModifier("Object Group");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "objectActiveOther";

        public override ModifierCategoryType Category => ModifierCategoryType.Enable;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IPrefabable prefabable)
                return false;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, modifierLoop.variables));
            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];
                if (bm.runtimeObject ? bm.runtimeObject.Active : bm.Alive)
                    return true;
            }
            return false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
        }

        #endregion
    }
}
