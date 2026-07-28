using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetCheckpointTime : ModifierVariableBase
    {
        #region Constructors

        public GetCheckpointTime() => SetupModifier("CHECKPOINT_TIME", "0");

        #endregion

        #region Values

        public override string Name => "getCheckpointTime";

        public override CategoryType Category => CategoryType.Checkpoints;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (GameData.Current.data.checkpoints.TryGetAt(modifier.GetInt(1, 0, modifierLoop.variables), out Checkpoint checkpoint))
                return checkpoint.time.ToString();
            return null;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.IntegerGenerator(modifier, reference, "Checkpoint Index", 1);
        }

        #endregion
    }
}
