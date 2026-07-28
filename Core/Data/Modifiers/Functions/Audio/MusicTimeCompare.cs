using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class MusicTimeCompare : ModifierTriggerBase
    {
        #region Constructors

        public MusicTimeCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "musicTime" + comparison.ToString();
            SetupModifier("0", "False");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Audio;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => comparison.Compare(AudioManager.inst.CurrentAudioSource.time - (modifier.GetBool(1, false, modifierLoop.variables) && modifierLoop.reference is ILifetime lifetime ? lifetime.StartTime : 0f), modifier.GetFloat(0, 0f, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Compare To", 0, 0f);
            modifierCard.BoolGenerator(modifier, reference, "Offset From Start Time", 1, false);
        }

        #endregion
    }
}
