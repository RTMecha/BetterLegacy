using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PitchCompare : ModifierTriggerBase
    {
        #region Constructors

        public PitchCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "pitch" + comparison.ToString();
            SetupModifier("1");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Audio;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => comparison.Compare(AudioManager.inst.pitch, modifier.GetFloat(0, 0f, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Compare To", 0, 1f);
        }

        #endregion
    }
}
