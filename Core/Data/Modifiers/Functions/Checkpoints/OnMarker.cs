using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class OnMarker : ModifierTriggerBase
    {
        #region Constructors

        public OnMarker() => SetupModifier("Marker Name", "-1", "-1");

        #endregion

        #region Values

        public override string Name => "onMarker";

        public override CategoryType Category => CategoryType.Checkpoints;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var forward = AudioManager.inst.CurrentAudioSource.pitch >= 0f;

            var name = modifier.GetValue(0, modifierLoop.variables);
            var color = modifier.GetInt(1, -1, modifierLoop.variables);
            var layer = modifier.GetInt(2, -1, modifierLoop.variables);
            var index = modifier.GetResultOrDefault(() => GameData.Current.data.GetLastMarkerIndex(x => x.Matches(name, color, layer)));
            var newIndex = GameData.Current.data.GetLastMarkerIndex(x => x.Matches(name, color, layer));
            if (index != newIndex)
            {
                modifier.Result = newIndex;
                // if current pitch is forwards, check if new index is ahead, otherwise if pitch is backwards then check if new index is behind
                return newIndex > index;
            }
            return false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Name", 0);
            modifierCard.ColorGenerator(modifier, reference, "Color", 1, MarkerEditor.inst.markerColors);
            modifierCard.IntegerGenerator(modifier, reference, "Layer", 2);
        }

        #endregion
    }
}
