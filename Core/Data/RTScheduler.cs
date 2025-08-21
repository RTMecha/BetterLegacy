using System;
using System.Collections.Generic;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Schedules an event to a specified time. Once the time is triggered, the event runs and is removed from the scheduled events.
    /// </summary>
    public class RTScheduler : Exists
    {
        /// <summary>
        /// List of scheduled events to run.
        /// </summary>
        List<Scheduledevent> scheduledEvents = new List<Scheduledevent>();

        /// <summary>
        /// Schedules an event.
        /// </summary>
        /// <param name="time">Time to run the function at.</param>
        /// <param name="action">Function to run when the time is in range.</param>
        public void Schedule(float time, Action action) => scheduledEvents.Add(new Scheduledevent(time, action));

        /// <summary>
        /// Ticks the scheduler.
        /// </summary>
        /// <param name="t">Time to trigger.</param>
        public void Tick(float t)
        {
            while (scheduledEvents.TryFindIndex(x => x.InRange(t), out int index))
                Trigger(index);
        }

        /// <summary>
        /// Triggers a scheduled event and removes it from the schedule.
        /// </summary>
        /// <param name="index">Index of the event.</param>
        public void Trigger(int index)
        {
            if (!scheduledEvents.InRange(index))
                return;

            scheduledEvents[index].action?.Invoke();
            scheduledEvents.RemoveAt(index);
        }

        /// <summary>
        /// Checks if the scheduler contains no elements.
        /// </summary>
        /// <returns>Returns true if the scheduler doesn't contain elements.</returns>
        public bool IsEmpty() => scheduledEvents.IsEmpty();

        /// <summary>
        /// Represents an event that is on the schedule.
        /// </summary>
        public class Scheduledevent
        {
            public Scheduledevent() { }

            public Scheduledevent(float time, Action action)
            {
                this.time = time;
                this.action = action;
            }
            
            public Scheduledevent(float time, float length, Action action)
            {
                this.time = time;
                this.length = length;
                this.action = action;
            }

            /// <summary>
            /// Time to trigger the scheduled event.
            /// </summary>
            public float time;

            /// <summary>
            /// Range the trigger should only be active in.
            /// </summary>
            public float length = float.MaxValue;

            /// <summary>
            /// Function to run when triggered.
            /// </summary>
            public Action action;

            /// <summary>
            /// Checks if the time is in the range of this scheduled event.
            /// </summary>
            /// <param name="t">Time to trigger.</param>
            /// <returns>Returns true if the time is in the range of the scheduled event, otherwise returns false.</returns>
            public bool InRange(float t) => t > time && t < length;
        }
    }
}
