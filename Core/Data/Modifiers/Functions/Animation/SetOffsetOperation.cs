using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetOffsetOperation : ModifierActionBase
    {
        #region Constructors

        public SetOffsetOperation() => SetupModifier("0", "0", "0");

        #endregion

        #region Values

        public override string Name => "setOffsetOperation";

        public override CategoryType Category => CategoryType.Animation;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;
            beatmapObject.PositionOperation = Parser.TryParse(modifier.GetValue(0), true, MathOperation.Addition);
            beatmapObject.ScaleOperation = Parser.TryParse(modifier.GetValue(1), true, MathOperation.Addition);
            beatmapObject.RotationOperation = Parser.TryParse(modifier.GetValue(2), true, MathOperation.Addition);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.DropdownGenerator(modifier, reference, "Pos Operation", 0, CoreHelper.ToOptionData<MathOperation>());
            modifierCard.DropdownGenerator(modifier, reference, "Sca Operation", 1, CoreHelper.ToOptionData<MathOperation>());
            modifierCard.DropdownGenerator(modifier, reference, "Rot Operation", 2, CoreHelper.ToOptionData<MathOperation>());
        }

        #endregion
    }
}
