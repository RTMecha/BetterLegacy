using SimpleJSON;
using System.Collections.Generic;
using BaseBeatmapData = DataManager.GameData.BeatmapData;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class LevelBeatmapData : BaseBeatmapData
    {
        public LevelBeatmapData()
        {

        }

        public static LevelBeatmapData ParseVG(JSONNode jn)
        {
            var beatmapData = new LevelBeatmapData
            {
                levelData = new Beatmap.LevelData(),
                editorData = new LevelEditorData(),
                markers = new List<Beatmap.Marker>()
            };

            for (int i = 0; i < jn["markers"].Count; i++)
                beatmapData.markers.Add(Beatmap.Marker.ParseVG(jn["markers"][i]));

            return beatmapData;
        }

        public static LevelBeatmapData Parse(JSONNode jn)
        {
            var beatmapData = new LevelBeatmapData
            {
                levelData = Beatmap.LevelData.Parse(jn["level_data"]),
                editorData = LevelEditorData.Parse(jn["ed"]),
                markers = new List<Beatmap.Marker>()
            };

            for (int i = 0; i < jn["ed"]["markers"].Count; i++)
                beatmapData.markers.Add(Beatmap.Marker.Parse(jn["ed"]["markers"][i]));

            return beatmapData;
        }

        public int GetLastCheckpointIndex()
        {
            var index = checkpoints.FindLastIndex(x => x.time < AudioManager.inst.CurrentAudioSource.time);
            return index < 0 ? 0 : index;
        }

        public Checkpoint GetLastCheckpoint() => checkpoints[GetLastCheckpointIndex()];

        public new List<Beatmap.Marker> markers = new List<Beatmap.Marker>();

        public new Beatmap.LevelData levelData;
        public new LevelEditorData editorData;
    }
}
