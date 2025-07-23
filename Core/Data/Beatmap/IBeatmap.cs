using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Data.Beatmap
{
    public interface IBeatmap
    {
        public Assets GetAssets();

        public List<BeatmapObject> BeatmapObjects { get; set; }

        public List<PrefabObject> PrefabObjects { get; set; }

        public List<Prefab> Prefabs { get; set; }

        public List<BackgroundObject> BackgroundObjects { get; set; }

        public List<BackgroundLayer> BackgroundLayers { get; set; }
    }
}
