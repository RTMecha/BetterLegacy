using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetRandomVector2 : ModifierActionBase
    {
        #region Constructors

        public GetRandomVector2() => SetupModifier("RANDOM_X_VAR", "RANDOM_Y_VAR", "1", "0", "0", "0", "0", "0");

        #endregion

        #region Values

        public override string Name => "getRandomVector2";

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var id = string.Empty;
            if (modifierLoop.reference is PAObjectBase obj)
                id = obj.id;

            var modifyable = modifierLoop.reference as IModifyable;

            var vector = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(id,
                randomType: (RandomType)modifier.GetInt(2, 0, modifierLoop.variables),
                valueX: modifier.GetFloat(3, 0f, modifierLoop.variables),
                valueY: modifier.GetFloat(4, 0f, modifierLoop.variables),
                randomValueX: modifier.GetFloat(5, 0f, modifierLoop.variables),
                randomValueY: modifier.GetFloat(6, 0f, modifierLoop.variables),
                interval: modifier.GetFloat(7, 0f, modifierLoop.variables),
                kfIndex: modifyable.Modifiers?.IndexOf(modifier) ?? 0);
            modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = vector.x.ToString();
            modifierLoop.variables[FormatStringVariables(modifier.GetValue(1), modifierLoop.variables)] = vector.y.ToString();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable X Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Variable Y Name", 1, renderVariables: false);
            modifierCard.DropdownGenerator(modifier, reference, "Random Type", 2, CoreHelper.StringToOptionData("None", "Normal", "BETA_SUPPORT", "Toggle", "Scale"));
            modifierCard.SingleGenerator(modifier, reference, "Value X", 3);
            modifierCard.SingleGenerator(modifier, reference, "Value Y", 4);
            modifierCard.SingleGenerator(modifier, reference, "Random Value X", 5);
            modifierCard.SingleGenerator(modifier, reference, "Random Value Y", 6);
            modifierCard.SingleGenerator(modifier, reference, "Random Interval", 7);
        }

        #endregion
    }
}
