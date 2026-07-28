using System.Collections.Generic;

using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class StoreLocalVariables : ModifierActionBase
    {
        #region Constructors

        public StoreLocalVariables() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "storeLocalVariables";

        public override CategoryType Category => CategoryType.Modifier;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.TryGetResult(out Dictionary<string, string> storedVariables))
            {
                modifierLoop.variables.InsertRange(storedVariables);
                return;
            }

            var storeVariables = new Dictionary<string, string>(modifierLoop.variables);
            modifier.Result = storeVariables;
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop) => modifier.Result = default;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
