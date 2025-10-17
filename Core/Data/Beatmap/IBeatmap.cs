using System.Collections.Generic;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Indicates a package of objects that build a level / beatmap.
    /// </summary>
    public interface IBeatmap
    {
        /// <summary>
        /// Gets the assets of the package.
        /// </summary>
        /// <returns>Returns the assets of the package.</returns>
        public Assets GetAssets();

        /// <summary>
        /// Beatmap Objects in the package.
        /// </summary>
        public List<BeatmapObject> BeatmapObjects { get; set; }

        /// <summary>
        /// Prefab Objects in the package.
        /// </summary>
        public List<PrefabObject> PrefabObjects { get; set; }

        /// <summary>
        /// Prefabs in the package.
        /// </summary>
        public List<Prefab> Prefabs { get; set; }

        /// <summary>
        /// Background Objects in the package.
        /// </summary>
        public List<BackgroundObject> BackgroundObjects { get; set; }

        /// <summary>
        /// Background Layers in the package.
        /// </summary>
        public List<BackgroundLayer> BackgroundLayers { get; set; }

        /// <summary>
        /// Beatmap Themes in the package.
        /// </summary>
        public List<BeatmapTheme> BeatmapThemes { get; set; }
    }
}
