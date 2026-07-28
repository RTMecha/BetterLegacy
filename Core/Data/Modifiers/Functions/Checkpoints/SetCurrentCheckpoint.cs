using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetCurrentCheckpoint : ModifierActionBase
    {
        #region Constructors

        public SetCurrentCheckpoint() => SetupModifier(false, "0");

        #endregion

        #region Values

        public override string Name => "setCurrentCheckpoint";

        public override CategoryType Category => CategoryType.Checkpoints;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop) => RTBeatmap.Current.SetCheckpoint(modifier.GetInt(0, 0, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Checkpoint Index", 0);
        }

        #endregion
    }
}
