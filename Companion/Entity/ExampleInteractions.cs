using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's interactions with the game.
    /// </summary>
    public class ExampleInteractions : ExampleModule
    {
        #region Default Instance

        /// <summary>
        /// The default interactions.
        /// </summary>
        public static ExampleInteractions Default
        {
            get
            {
                var interactions = new ExampleInteractions();
                interactions.InitDefault();
                return interactions;
            }
        }

        public override void InitDefault()
        {

        }

        #endregion

        #region Interactions

        /// <summary>
        /// When Example's head is clicked.
        /// </summary>
        public const string PET = "Pet";
        /// <summary>
        /// When you chat with Example.
        /// </summary>
        public const string CHAT = "Chat";
        /// <summary>
        /// When you hold one of Example's hands.
        /// </summary>
        public const string HOLD_HAND = "Hold Hand";
        /// <summary>
        /// When you touch Example's tail. Why would you do that.
        /// </summary>
        public const string TOUCHIE = "Touchie";
        /// <summary>
        /// When you interupt Example while he's dancing. Bruh.
        /// </summary>
        public const string INTERRUPT = "Interrupt";

        #endregion

        #region Core

        public override void Build()
        {

        }

        public override void Tick()
        {
            if (ProjectPlanner.inst && reference && reference.chatBubble && reference.brain && !reference.brain.talking)
                foreach (var schedule in ProjectPlanner.inst.schedules)
                {
                    if (!schedule.hasBeenChecked && schedule.IsActive)
                    {
                        schedule.hasBeenChecked = true;
                        reference.chatBubble.Say($"Reminding you about your schedule \"{schedule.Description}\" at {schedule.DateTime}");
                        ProjectPlanner.inst.SaveSchedules();
                    }
                }

        }

        public override void Clear()
        {
            attributes.Clear();
        }

        #endregion
    }
}
