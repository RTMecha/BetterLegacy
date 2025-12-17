using System.Collections.Generic;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Runtime.Objects
{
    /// <summary>
    /// Object animation engine class.
    /// </summary>
    public class ObjectEngine : Exists
    {
        /// <summary>
        /// Sorts active and deactive objects. Only active objects will be interpolated.
        /// </summary>
        public readonly ObjectSpawner spawner;

        /// <summary>
        /// If the engine should recalculate.
        /// </summary>
        public bool shouldRecalculate;

        public ObjectEngine(IReadOnlyList<IRTObject> objects) => spawner = new ObjectSpawner(objects);

        /// <summary>
        /// Updates all objects based on <paramref name="time"/>.
        /// </summary>
        /// <param name="time">Time since the level started.</param>
        public void Update(float time)
        {
            if (shouldRecalculate)
                spawner.RecalculateObjectStates();
            shouldRecalculate = false;

            spawner.Update(time);

            foreach (IRTObject runtimeObject in spawner.ActiveObjects)
                runtimeObject.Interpolate(time);
        }

        /// <summary>
        /// Queues the engine to recalculate object states before spawner is updated.
        /// </summary>
        public void Recalculate() => shouldRecalculate = true;
    }
}