using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ResetCheckpoint : ModifierActionBase
    {
        #region Constructors

        public ResetCheckpoint() => SetupModifier(false, "False");

        #endregion

        #region Values

        public override string Name => "resetCheckpoint";

        public override CategoryType Category => CategoryType.Checkpoints;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            RTBeatmap.Current.ResetCheckpoint(modifier.GetBool(0, false, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.BoolGenerator(modifier, reference, "Reset to Previous", 0);
        }

        #endregion
    }
}
