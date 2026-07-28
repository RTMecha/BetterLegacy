using UnityEngine;

using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class AwaitCounter : ModifierTriggerBase
    {
        #region Constructors

        public AwaitCounter() => SetupModifier("0", "10", "1");

        #endregion

        #region Values

        public override string Name => "awaitCounter";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var start = modifier.GetInt(0, 0, modifierLoop.variables);
            var end = modifier.GetInt(1, 10, modifierLoop.variables);
            var num = modifier.GetResultOrDefault(() => start - 1);
            num = Mathf.FloorToInt(num + (modifier.GetInt(2, 1, modifierLoop.variables) * Time.deltaTime));
            modifier.Result = num;
            return num >= end;
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop) => modifier.Result = default;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Start", 0);
            modifierCard.IntegerGenerator(modifier, reference, "End", 1);
            modifierCard.IntegerGenerator(modifier, reference, "Amount", 2);
        }

        #endregion
    }
}
