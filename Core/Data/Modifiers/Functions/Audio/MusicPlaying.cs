using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class MusicPlaying : ModifierTriggerBase
    {
        #region Constructors

        public MusicPlaying() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "musicPlaying";

        public override CategoryType Category => CategoryType.Audio;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => AudioManager.inst.CurrentAudioSource.isPlaying;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
