using BetterLegacy.Editor.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Companion.Entity
{
    public class ExampleInteractions : ExampleModule
    {
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
    }
}
