using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class FromPrefab : ModifierTriggerBase
    {
        #region Constructors

        public FromPrefab() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "fromPrefab";

        public override ModifierCategoryType Category => ModifierCategoryType.Prefab;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => modifierLoop.reference is IPrefabable prefabable && prefabable.FromPrefab;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
