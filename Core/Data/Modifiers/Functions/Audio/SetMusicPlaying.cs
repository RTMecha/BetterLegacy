using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetMusicPlaying : ModifierActionBase
    {
        #region Constructors

        public SetMusicPlaying() => SetupModifier(false, "False");

        #endregion

        #region Values

        public override string Name => "setMusicPlaying";

        public override CategoryType Category => CategoryType.Audio;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop) => SoundManager.inst.SetPlaying(modifier.GetBool(0, false, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.BoolGenerator(modifier, reference, "Playing", 0, false);
        }

        #endregion
    }
}
