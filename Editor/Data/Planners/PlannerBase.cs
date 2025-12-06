using UnityEngine;

using LSFunctions;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Data.Planners
{
    /// <summary>
    /// Base Planner class.
    /// </summary>
    public abstract class PlannerBase : Exists
    {
        public PlannerBase(Type type) { PlannerType = type; }

        public string ID { get; set; } = LSText.randomNumString(10);

        public GameObject GameObject { get; set; }

        public Type PlannerType { get; set; }

        public enum Type
        {
            Document,
            TODO,
            Character,
            Timeline,
            Schedule,
            Note,
            OST
        }

        /// <summary>
        /// Initializes the planner item.
        /// </summary>
        public abstract void Init();

        public abstract bool SamePlanner(PlannerBase other);
    }
}
