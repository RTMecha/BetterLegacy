using System.Collections.Generic;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    // TODO: please make this better (intended behavior is something like onMarker or onCheckpoint)
    public class ObjectSpawned : ModifierTriggerBase
    {
        #region Constructors

        public ObjectSpawned()
        {
            SetupModifier("Object Group");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "objectSpawned";

        public override ModifierCategoryType Category => ModifierCategoryType.Enable;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IPrefabable prefabable)
                return false;

            if (!modifier.TryGetResult(out List<string> ids))
            {
                ids = new List<string>();
                modifier.Result = ids;
            }

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, modifierLoop.variables));
            for (int i = 0; i < list.Count; i++)
            {
                if (!ids.Contains(list[i].id) && list[i].Alive)
                {
                    ids.Add(list[i].id);
                    modifier.Result = ids;
                    return true;
                }
            }
            return false;
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop) => modifier.Result = default;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
        }

        #endregion
    }
}
