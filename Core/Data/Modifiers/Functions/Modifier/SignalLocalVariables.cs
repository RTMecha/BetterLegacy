using System.Collections.Generic;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SignalLocalVariables : ModifierActionBase
    {
        #region Constructors

        public SignalLocalVariables()
        {
            SetupModifier("Object Group");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "signalLocalVariables";

        public override ModifierCategoryType Category => ModifierCategoryType.Modifier;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullBeatmapCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables));
            if (list.IsEmpty())
                return;

            var sendVariables = new Dictionary<string, string>(modifierLoop.variables);

            foreach (var beatmapObject in list)
                beatmapObject.modifiers.FindAll(x => x.Name == "getSignaledVariables").ForLoop(modifier =>
                {
                    if (modifier.TryGetResult(out Dictionary<string, string> otherVariables))
                    {
                        otherVariables.InsertRange(modifierLoop.variables);
                        return;
                    }

                    modifier.Result = sendVariables;
                });
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
        }

        #endregion
    }
}
