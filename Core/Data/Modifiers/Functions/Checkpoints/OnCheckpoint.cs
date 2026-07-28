using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class OnCheckpoint : ModifierTriggerBase
    {
        #region Constructors

        public OnCheckpoint() => SetupModifier("Checkpoint Name");

        #endregion

        #region Values

        public override string Name => "onCheckpoint";

        public override CategoryType Category => CategoryType.Checkpoints;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var forward = AudioManager.inst.CurrentAudioSource.pitch >= 0f;

            var name = modifier.GetValue(0, modifierLoop.variables);
            var index = modifier.GetResultOrDefault(() => GameData.Current.data.GetLastCheckpointIndex(x => string.IsNullOrEmpty(name) || x.name == name));
            var newIndex = GameData.Current.data.GetLastCheckpointIndex(x => string.IsNullOrEmpty(name) || x.name == name);
            if (index != newIndex)
            {
                modifier.Result = newIndex;
                return newIndex > index;
            }
            return false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Name", 0);
        }

        #endregion
    }
}
