using BetterLegacy.Core.Data;
using BetterLegacy.Core.Optimization.Objects;
using System;

namespace BetterLegacy.Core.Optimization.Level
{
    /// <summary>
    /// Main animation engine class.
    /// </summary>
    public class Engine : Exists, IDisposable
    {
        public readonly ObjectSpawner objectSpawner;
        public readonly LevelStorage level;

        public Engine(LevelStorage level)
        {
            this.level = level;
            objectSpawner = new ObjectSpawner(level.Objects);

            level.ObjectInserted += OnObjectInserted;
            level.ObjectRemoved += OnObjectRemoved;
        }

        public void Update(float time)
        {
            objectSpawner.Update(time);

            foreach (ILevelObject levelObject in objectSpawner.ActiveObjects)
            {
                levelObject.Interpolate(time);
            }
        }

        private void OnObjectInserted(object sender, ILevelObject e)
        {
            objectSpawner.InsertObject(e);
        }

        private void OnObjectRemoved(object sender, ILevelObject e)
        {
            objectSpawner.RemoveObject(e);
        }

        public void Dispose()
        {
            level.ObjectInserted -= OnObjectInserted;
            level.ObjectRemoved -= OnObjectRemoved;
        }
    }
}