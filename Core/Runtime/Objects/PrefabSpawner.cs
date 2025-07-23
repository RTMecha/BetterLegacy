using System.Collections.Generic;

using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Runtime.Objects
{
    /// <summary>
    /// Class for storing objects spawned from a Prefab Object.
    /// </summary>
    public class PrefabSpawner : IBeatmap
    {
        /// <summary>
        /// Clears the spawned objects.
        /// </summary>
        public void Clear()
        {
            BeatmapObjects.Clear();
            PrefabObjects.Clear();
            Prefabs.Clear();
            BackgroundObjects.Clear();
            BackgroundLayers.Clear();
        }

        public Assets GetAssets() => null;

        public List<BeatmapObject> BeatmapObjects { get; set; } = new List<BeatmapObject>();

        public List<PrefabObject> PrefabObjects { get; set; } = new List<PrefabObject>();

        public List<Prefab> Prefabs { get; set; } = new List<Prefab>();

        public List<BackgroundObject> BackgroundObjects { get; set; } = new List<BackgroundObject>();

        public List<BackgroundLayer> BackgroundLayers { get; set; } = new List<BackgroundLayer>();
    }
}
