using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetCheckpointCount : ModifierVariableBase
    {
        #region Constructors

        public GetCheckpointCount() => SetupModifier("CHECKPOINT_COUNT");

        #endregion

        #region Values

        public override string Name => "getCheckpointCount";

        public override ModifierCategoryType Category => ModifierCategoryType.Checkpoints;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => GameData.Current.data.checkpoints.Count.ToString();

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
        }

        #endregion
    }
}
