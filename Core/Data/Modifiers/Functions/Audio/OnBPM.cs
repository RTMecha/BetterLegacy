using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    // TODO: finish this modifier.
    // the intention is this will trigger on each beat for either a frame (instant) or a duration.
    public class OnBPM : ModifierTriggerBase
    {
        #region Constructors

        public OnBPM() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "onBPM";

        public override ModifierCategoryType Category => ModifierCategoryType.Audio;

        public override bool DisplayInEditor => false; // wip

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            //    var bpm = modifier.GetFloat(0, 0f, modifierLoop.variables);
            //    var measures = modifier.GetFloat(1, 4f, modifierLoop.variables);
            //    var range = modifier.GetFloat(2, 0f, modifierLoop.variables);

            //    var bpmMulti = 60f / bpm;
            //    var currentBeat = Mathf.FloorToInt(RTLevel.Current.FixedTime / bpmMulti);
            //    var modulo = RTLevel.Current.FixedTime % bpmMulti;

            return false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}
