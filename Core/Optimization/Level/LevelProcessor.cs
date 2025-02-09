using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Optimization.Objects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterLegacy.Core.Optimization.Level
{
    public class LevelProcessor : Exists, IDisposable
    {
        public readonly LevelStorage level;
        public readonly Engine engine;
        public readonly ObjectConverter converter;

        public LevelProcessor(GameData gameData)
        {
            // Convert GameData to LevelObjects
            converter = new ObjectConverter(gameData);
            IEnumerable<ILevelObject> levelObjects = converter.ToLevelObjects();

            level = new LevelStorage(levelObjects);
            engine = new Engine(level);

            Debug.Log($"{Updater.className}Loaded {level.Objects.Count} objects (original: {gameData.beatmapObjects.Count})");
        }

        public void Update(float time) => engine?.Update(time);

        public void Dispose() => engine.Dispose();
    }
}
