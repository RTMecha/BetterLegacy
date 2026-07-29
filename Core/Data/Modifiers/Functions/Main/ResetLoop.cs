using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ResetLoop : ModifierActionBase
    {
        #region Constructors

        public ResetLoop() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "resetLoop";

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        public override bool OverrideRunningState => true;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not IModifyable modifyable)
                return;

            var runCount = modifier.runCount;
            if (!modifier.running)
                runCount++;

            modifier.running = true;

            if (!(modifier.active || !modifierLoop.state.result || modifier.triggerCount > 0 && runCount >= modifier.triggerCount))
                modifyable.Modifiers.ForLoop(modifier =>
                {
                    if (modifier.compatibility.StoryOnly && !ProjectArrhythmia.State.InStory || !modifier.active && !modifier.running)
                        return;

                    modifier.Reset(modifierLoop);
                });

            modifier.runCount = runCount;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}
