using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetObjectName : ModifierVariableBase
    {
        #region Constructors

        public GetObjectName()
        {

        }

        #endregion

        #region Values

        public override string Name => "getObjectName";

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject().WithPrefabObject().WithPlayerObject();

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is BeatmapObject beatmapObject)
                return beatmapObject.name;
            else if (modifierLoop.reference is BackgroundObject backgroundObject)
                return backgroundObject.name;
            else if (modifierLoop.reference is PrefabObject prefabObject && prefabObject.GetPrefab() is Prefab prefab)
                return prefab.name;
            else if (modifierLoop.reference is RTCustomPlayerObject customPlayerObject && customPlayerObject.reference)
                return customPlayerObject.reference.name;
            return null;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
        }

        #endregion
    }
}
