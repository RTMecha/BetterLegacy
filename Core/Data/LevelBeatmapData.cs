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

            beatmapData.editorData = new LevelEditorData();

            beatmapData.markers = new List<Marker>();

            for (int i = 0; i < jn["markers"].Count; i++)
            {
                var jnmarker = jn["markers"][i];

                var name = jnmarker["n"] == null ? "Marker" : (string)jnmarker["n"];

                var desc = jnmarker["d"] == null ? "" : (string)jnmarker["d"];

                var col = jnmarker["c"].AsInt;

                var time = jnmarker["t"].AsFloat;

                beatmapData.markers.Add(new Marker(true, name, desc, col, time));
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
            {
                bool active = jn["ed"]["markers"][i]["active"].AsBool;
                string name = "";
                if (jn["ed"]["markers"][i]["name"] != null)
                    name = jn["ed"]["markers"][i]["name"];

                string desc = "";
                if (jn["ed"]["markers"][i]["desc"] != null)
                    desc = jn["ed"]["markers"][i]["desc"];

                float time = jn["ed"]["markers"][i]["t"].AsFloat;

                int color = 0;
                if (jn["ed"]["markers"][i]["col"] != null)
                    color = jn["ed"]["markers"][i]["col"].AsInt;

                beatmapData.markers.Add(new Marker(active, name, desc, color, time));
            }
            return beatmapData;
        }

        public Data.LevelData ModLevelData => (Data.LevelData)levelData;
        public LevelEditorData ModEditorData => (LevelEditorData)editorData;
    }
}
