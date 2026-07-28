using System.Collections.Generic;

using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetSignaledVariables : ModifierActionBase
    {
        #region Constructors

        public GetSignaledVariables() => SetupModifier("True");

        #endregion

        #region Values

        public override string Name => "getSignaledVariables";

        public override CategoryType Category => CategoryType.Modifier;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!modifier.TryGetResult(out Dictionary<string, string> otherVariables))
                return;

            foreach (var variable in otherVariables)
                modifierLoop.variables[variable.Key] = variable.Value;

            if (!modifier.GetBool(0, true, modifierLoop.variables)) // don't clear
                return;

            otherVariables.Clear();
            modifier.Result = null;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.BoolGenerator(modifier, reference, "Clear", 0, true);
        }

        #endregion
    }
}
