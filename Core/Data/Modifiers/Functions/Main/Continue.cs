using UnityEngine;

using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class Continue : ModifierActionBase
    {
        #region Constructors

        public Continue(bool isReturn)
        {
            this.isReturn = isReturn;
            Name = isReturn ? "return" : "continue";
            SetupModifier();
            Modifier.collapse = true;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        public override bool SpecialFunction => true;

        readonly bool isReturn;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            // Set modifier inactive state
            if (!modifierLoop.state.result && !(!modifier.active && !modifier.running))
            {
                modifier.active = false;
                modifier.running = false;
                modifierLoop.state.result = false;
            }

            if (modifier.active || !modifierLoop.state.result || modifier.triggerCount > 0 && modifier.runCount >= modifier.triggerCount) // don't return
                modifierLoop.state.result = false;

            if (!modifier.running)
                modifier.runCount = Mathf.FloorToInt(modifier.runCount + (1 * Time.deltaTime));

            // Only occur once
            if (!modifier.constant && modifierLoop.state.sequence + 1 >= modifierLoop.state.end)
                modifier.active = true;

            modifier.running = modifierLoop.state.result;

            if (modifierLoop.state.result)
            {
                modifierLoop.state.continued = true;
                modifierLoop.state.returned = isReturn;
            }

            modifierLoop.state.result = true;

            modifierLoop.state.previousType = modifier.type;
            modifierLoop.state.index++;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
