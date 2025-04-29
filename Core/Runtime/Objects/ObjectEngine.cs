using System;
using System.Collections.Generic;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Runtime.Objects
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