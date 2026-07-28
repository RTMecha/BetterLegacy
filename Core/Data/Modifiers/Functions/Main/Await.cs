using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class Await : ModifierTriggerBase
    {
        #region Constructors

        public Await() => SetupModifier("0", "1", "True");

        #endregion

        #region Values

        public override string Name => "await";

        public override CategoryType Category => CategoryType.Main;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!modifier.constant)
            {
                if (ProjectArrhythmia.State.InEditor)
                    EditorManager.inst.DisplayNotification($"Constant has to be on in order for await modifiers to work!", 4f, EditorManager.NotificationType.Error);
                return false;
            }

            var start = modifier.GetInt(0, 0, modifierLoop.variables);
            var realTime = modifier.GetBool(2, true, modifierLoop.variables);
            float time;
            if (realTime)
            {
                var timer = modifier.GetResultOrDefault(() =>
                {
                    var timer = new RTTimer();
                    timer.offset = start;
                    timer.Reset();
                    return timer;
                });
                timer.Update();
                time = timer.time;
            }
            else
                time = modifier.GetResultOrDefault(() => modifierLoop.reference.GetParentRuntime().FixedTime + start) + modifierLoop.reference.GetParentRuntime().FixedTime;

            return time >= modifier.GetFloat(1, 0f, modifierLoop.variables);
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop) => modifier.Result = default;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Start Time", 0);
            modifierCard.SingleGenerator(modifier, reference, "Trigger Time", 1);
            modifierCard.BoolGenerator(modifier, reference, "Use Real Time", 2);
        }

        #endregion
    }
}
