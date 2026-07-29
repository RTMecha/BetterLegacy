using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetCheckpointIndex : ModifierVariableBase
    {
        #region Constructors

        public GetCheckpointIndex(Type type)
        {
            this.type = type;
            Name = "get" + type.ToString() + "CheckpointIndex";
            SetupModifier($"{type.ToString().ToUpper()}_CHECKPOINT");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Checkpoints;

        readonly Type type;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => type switch
        {
            Type.Active => RTBeatmap.Current.ActiveCheckpointIndex.ToString(),
            Type.Last => GameData.Current.data.GetLastCheckpointIndex().ToString(),
            Type.Next => GameData.Current.data.GetNextCheckpointIndex().ToString(),
            _ => null,
        };

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
        }

        #endregion

        #region Sub Classes

        public enum Type
        {
            Active,
            Last,
            Next,
        }

        #endregion
    }
}
