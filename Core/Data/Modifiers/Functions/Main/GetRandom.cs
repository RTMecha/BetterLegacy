using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetRandom : ModifierVariableBase
    {
        #region Constructors

        public GetRandom() => SetupModifier("RANDOM_VAR", "1", "0", "0", "0", "0");

        #endregion

        #region Values

        public override string Name => "getRandom";

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            var id = string.Empty;
            if (modifierLoop.reference is PAObjectBase obj)
                id = obj.id;

            var modifyable = modifierLoop.reference as IModifyable;

            return RandomHelper.KeyframeRandomizer.RandomizeFloatKeyframe(id,
                randomType: (RandomType)modifier.GetInt(1, 0, modifierLoop.variables),
                value: modifier.GetFloat(2, 0f, modifierLoop.variables),
                randomValueA: modifier.GetFloat(3, 0f, modifierLoop.variables),
                randomValueB: modifier.GetFloat(4, 0f, modifierLoop.variables),
                interval: modifier.GetFloat(5, 0f, modifierLoop.variables),
                kfIndex: modifyable.Modifiers?.IndexOf(modifier) ?? 0).ToString();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.DropdownGenerator(modifier, reference, "Random Type", 1, CoreHelper.StringToOptionData("None", "Normal", "BETA_SUPPORT", "Toggle", "Scale"));
            modifierCard.SingleGenerator(modifier, reference, "Value", 2);
            modifierCard.SingleGenerator(modifier, reference, "Random Value A", 3);
            modifierCard.SingleGenerator(modifier, reference, "Random Value B", 4);
            modifierCard.SingleGenerator(modifier, reference, "Random Interval", 5);
        }

        #endregion
    }
}
