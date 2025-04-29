using System;
using System.Collections.Generic;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Optimization.Objects;

namespace BetterLegacy.Core.Optimization.Level
{
    /// <summary>
    /// Main animation engine class.
    /// </summary>
    public class ObjectEngine : Exists
    {
        public readonly ObjectSpawner objectSpawner;

        public ObjectEngine(IReadOnlyList<IRTObject> objects) => objectSpawner = new ObjectSpawner(objects);

        public void Update(float time)
        {
            objectSpawner.Update(time);

            foreach (IRTObject levelObject in objectSpawner.ActiveObjects)
                levelObject.Interpolate(time);
        }
    }
}