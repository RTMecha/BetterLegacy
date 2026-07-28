using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetKeyframeValue : ModifierVariableBase
    {
        #region Constructors

        public GetKeyframeValue() => SetupModifier("KEYFRAME_VALUE_VAR", "0", "0", "0", "0");

        #endregion

        #region Values

        public override string Name => "getKeyframeValue";

        public override CategoryType Category => CategoryType.Animation;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Values

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return null;

            var source = modifier.GetInt(1, 0, modifierLoop.variables);
            var type = modifier.GetInt(2, 0, modifierLoop.variables);
            var valueIndex = modifier.GetInt(3, 0, modifierLoop.variables);
            var time = modifier.GetFloat(4, 0f, modifierLoop.variables);
            return beatmapObject.Interpolate(
                type: type,
                valueIndex: valueIndex,
                time: time,
                source: source).ToString();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

            modifierCard.DropdownGenerator(modifier, reference, "Source", 1, CoreHelper.StringToOptionData("Normal", "Random"));
            modifierCard.DropdownGenerator(modifier, reference, "Type", 2, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
            modifierCard.IntegerGenerator(modifier, reference, "Value Index", 3);
            modifierCard.SingleGenerator(modifier, reference, "Time", 4);
        }

        #endregion
    }
}
