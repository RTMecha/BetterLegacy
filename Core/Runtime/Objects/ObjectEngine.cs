using System;
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

        public ObjectEngine(IReadOnlyList<IRTObject> objects) => spawner = new ObjectSpawner(objects);

        /// <summary>
        /// Updates all objects based on <paramref name="time"/>.
        /// </summary>
        /// <param name="time">Time since the level started.</param>
        public void Update(float time)
        {
            spawner.Update(time);

            foreach (IRTObject levelObject in spawner.ActiveObjects)
                levelObject.Interpolate(time);
        }
    }
}