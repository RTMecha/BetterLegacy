using SimpleJSON;
using System.Collections.Generic;
using BaseBeatmapData = DataManager.GameData.BeatmapData;

namespace BetterLegacy.Core.Data
{
    public class LevelBeatmapData : BaseBeatmapData
    {
        public LevelBeatmapData()
        {

        }

        public static LevelBeatmapData ParseVG(JSONNode jn)
        {
            var beatmapData = new LevelBeatmapData();

            beatmapData.levelData = new Data.LevelData();
            beatmapData.editorData = new LevelEditorData();

            beatmapData.markers = new List<Marker>();

            for (int i = 0; i < jn["markers"].Count; i++)
            {
                var jnmarker = jn["markers"][i];

                var name = jnmarker["n"] == null ? "Marker" : (string)jnmarker["n"];

                var desc = jnmarker["d"] == null ? "" : (string)jnmarker["d"];

                var col = jnmarker["c"].AsInt;

                var time = jnmarker["t"].AsFloat;

                beatmapData.markers.Add(new Data.Marker(true, name, desc, col, time));
            }
            return beatmapData;
        }

        public static LevelBeatmapData Parse(JSONNode jn)
        {
            var beatmapData = new LevelBeatmapData();

            beatmapData.levelData = Data.LevelData.Parse(jn["level_data"]);
            beatmapData.editorData = LevelEditorData.Parse(jn["ed"]);

            beatmapData.markers = new List<Marker>();
            for (int i = 0; i < jn["ed"]["markers"].Count; i++)
                beatmapData.markers.Add(Data.Marker.Parse(jn["ed"]["markers"][i]));

            return beatmapData;
        }

        public Data.LevelData ModLevelData => (Data.LevelData)levelData;
        public LevelEditorData ModEditorData => (LevelEditorData)editorData;
    }
}
