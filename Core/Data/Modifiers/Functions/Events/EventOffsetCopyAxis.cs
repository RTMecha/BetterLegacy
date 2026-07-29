using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class EventOffsetCopyAxis : ModifierActionBase
    {
        #region Constructor

        public EventOffsetCopyAxis() => SetupModifier("0", "0", "0", "0", "0", "0", "1", "0", "-99999", "99999", "99999", "False", "0");

        #endregion

        #region Values

        public override string Name => "eventOffsetCopyAxis";

        public override ModifierCategoryType Category => ModifierCategoryType.Events;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!RTLevel.Current.eventEngine || RTLevel.Current.eventEngine.offsets == null || modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var fromType = modifier.GetInt(1, 0, modifierLoop.variables);
            var fromAxis = modifier.GetInt(2, 0, modifierLoop.variables);
            var toType = modifier.GetInt(3, 0, modifierLoop.variables);
            var toAxis = modifier.GetInt(4, 0, modifierLoop.variables);
            var delay = modifier.GetFloat(5, 0f, modifierLoop.variables);
            var multiply = modifier.GetFloat(6, 0f, modifierLoop.variables);
            var offset = modifier.GetFloat(7, 0f, modifierLoop.variables);
            var min = modifier.GetFloat(8, 0f, modifierLoop.variables);
            var max = modifier.GetFloat(9, 0f, modifierLoop.variables);
            var loop = modifier.GetFloat(10, 0f, modifierLoop.variables);
            var useVisual = modifier.GetBool(11, false, modifierLoop.variables);
            var operation = Parser.TryParse(modifier.GetValue(12, modifierLoop.variables), true, MathOperation.Addition);

            var time = AudioManager.inst.CurrentAudioSource.time;

            fromType = RTMath.Clamp(fromType, 0, beatmapObject.events.Count - 1);
            fromAxis = RTMath.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length - 1);
            toType = RTMath.Clamp(toType, 0, RTLevel.Current.eventEngine.offsets.Count - 1);
            toAxis = RTMath.Clamp(toAxis, 0, RTLevel.Current.eventEngine.offsets[toType].Count - 1);

            if (!useVisual && beatmapObject.cachedSequences)
            {
                RTLevel.Current.eventEngine.SetOffset(toType, toAxis, fromType switch
                {
                    0 => RTMath.Clamp((beatmapObject.cachedSequences.PositionSequence.GetValue(time - beatmapObject.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    1 => RTMath.Clamp((beatmapObject.cachedSequences.ScaleSequence.GetValue(time - beatmapObject.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    2 => RTMath.Clamp((beatmapObject.cachedSequences.RotationSequence.GetValue(time - beatmapObject.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    _ => 0f,
                });
                RTLevel.Current.eventEngine.SetOffsetOperation(toType, toAxis, operation);
            }
            else if (beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
            {
                RTLevel.Current.eventEngine.SetOffset(toType, toAxis, RTMath.Clamp((runtimeObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max));
                RTLevel.Current.eventEngine.SetOffsetOperation(toType, toAxis, operation);
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
            modifierCard.DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

            modifierCard.DropdownGenerator(modifier, reference, "To Type", 3, CoreHelper.StringToOptionData(EventLibrary.displayNames), _val =>
            {
                modifier.SetValue(3, _val.ToString());
                modifier.SetValue(4, "0");
                modifierCard.RenderModifier(reference);
                modifierCard.Update(modifier, reference);
            });
            modifierCard.DropdownGenerator(modifier, reference, "To Axis", 4, CoreHelper.StringToOptionData(EventLibrary.valueNames[RTMath.Clamp(modifier.GetInt(3, 0), 0, EventLibrary.valueNames.Length - 1)]));

            modifierCard.SingleGenerator(modifier, reference, "Delay", 5, 0f);

            modifierCard.SingleGenerator(modifier, reference, "Multiply", 6, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Offset", 7, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Min", 8, -99999f);
            modifierCard.SingleGenerator(modifier, reference, "Max", 9, 99999f);

            modifierCard.SingleGenerator(modifier, reference, "Loop", 10, 99999f);
            modifierCard.BoolGenerator(modifier, reference, "Use Visual", 11, false);
            modifierCard.DropdownGenerator(modifier, reference, "Operation", 12, CoreHelper.ToOptionData<MathOperation>());
        }

        #endregion
    }
}
