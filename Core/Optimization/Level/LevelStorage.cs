using BetterLegacy.Core.Data;
using BetterLegacy.Core.Optimization.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterLegacy.Core.Optimization.Level
{
    /// <summary>
    /// Contains level data.
    /// </summary>
    public class LevelStorage : Exists
    {
        public IReadOnlyList<ILevelObject> Objects => objects.AsReadOnly();

        public List<ILevelObject> objects;

        public event EventHandler<ILevelObject>? ObjectInserted;
        public event EventHandler<ILevelObject>? ObjectRemoved;

        public LevelStorage()
        {
            objects = new List<ILevelObject>();
        }

        public LevelStorage(IEnumerable<ILevelObject> levelObjects)
        {
            objects = levelObjects.ToList();
        }

        public void InsertObject(ILevelObject levelObject)
        {
            objects.Add(levelObject);
            ObjectInserted?.Invoke(this, levelObject);
        }

        public void RemoveObject(ILevelObject levelObject)
        {
            objects.Remove(levelObject);
            ObjectRemoved?.Invoke(this, levelObject);
        }
    }
}