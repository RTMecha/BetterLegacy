using System;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ForLoop : ModifierActionBase
    {
        #region Constructors

        public ForLoop() => SetupModifier("INDEX_VAR", "0", "10", "1");

        #endregion

        #region Values

        public override string Name => "forLoop";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IModifyable modifyable)
                return;

            var modifiers = modifyable.Modifiers;

            var variable = FormatStringVariables(modifier.GetValue(0), modifierLoop.variables);
            var startIndex = modifier.GetInt(1, 0, modifierLoop.variables);
            var endCount = modifier.GetInt(2, 0, modifierLoop.variables);
            var increment = modifier.GetInt(3, 1, modifierLoop.variables);

            var allowed = increment != 0 && endCount > startIndex;

            var endIndex = modifiers.FindLastIndex(x => x.Name == "return"); // return is treated as a break of the for loop
            endIndex = endIndex <= modifierLoop.state.index ? modifiers.Count : endIndex;

            try
            {
                // if result is false, then skip the for loop sequence.
                if (allowed)
                {
                    var selectModifiers = modifiers.GetIndexRange(modifierLoop.state.index + 1, endIndex);
                    var innerLoop = modifier.GetResultOrDefault(() => new ModifierLoop(modifierLoop.reference, modifierLoop.variables));

                    if (increment > 0)
                        for (int i = startIndex; i < endCount; i += increment)
                        {
                            innerLoop.variables[variable] = i.ToString();
                            innerLoop.RunModifiersLoop(selectModifiers, i, endCount);
                        }
                    else
                        for (int i = endCount - 1; i >= startIndex; i -= increment)
                        {
                            innerLoop.variables[variable] = i.ToString();
                            innerLoop.RunModifiersLoop(selectModifiers, i, endCount);
                        }
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Had an exception with the forLoop modifier.\n" +
                    $"Index: {modifierLoop.state.index}\n" +
                    $"End Index: {endIndex}\nException: {ex}");
            }

            modifierLoop.state.index = endIndex; // exit for loop.
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.IntegerGenerator(modifier, reference, "Start Index", 1, 0);
            modifierCard.IntegerGenerator(modifier, reference, "End Count", 2, 10);
            modifierCard.IntegerGenerator(modifier, reference, "Increment", 3, 1);
        }

        #endregion
    }
}
