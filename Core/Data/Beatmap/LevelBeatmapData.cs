﻿using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class LevelBeatmapData : Exists
    {
        public LevelBeatmapData() { }

        public static LevelBeatmapData ParseVG(JSONNode jn)
        {
            var beatmapData = new LevelBeatmapData
            {
                level = new LevelData(),
                editor = new LevelEditorData(),
                markers = new List<Marker>()
            };

            for (int i = 0; i < jn["markers"].Count; i++)
                beatmapData.markers.Add(Marker.ParseVG(jn["markers"][i]));
            
            for (int i = 0; i < jn["checkpoints"].Count; i++)
                beatmapData.checkpoints.Add(Checkpoint.ParseVG(jn["checkpoints"][i]));
            if (beatmapData.checkpoints.IsEmpty())
                beatmapData.checkpoints.Add(Checkpoint.Default);

            beatmapData.markers = beatmapData.markers.OrderBy(x => x.time).ToList();
            beatmapData.checkpoints = beatmapData.checkpoints.OrderBy(x => x.time).ToList();

            return beatmapData;
        }

        public static LevelBeatmapData Parse(JSONNode jn)
        {
            var beatmapData = new LevelBeatmapData
            {
                level = LevelData.Parse(jn["level_data"]),
                editor = LevelEditorData.Parse(jn["ed"]),
                markers = new List<Marker>(),
            };

            for (int i = 0; i < jn["ed"]["markers"].Count; i++)
                beatmapData.markers.Add(Marker.Parse(jn["ed"]["markers"][i]));

            for (int i = 0; i < jn["checkpoints"].Count; i++)
                beatmapData.checkpoints.Add(Checkpoint.Parse(jn["checkpoints"][i]));
            if (beatmapData.checkpoints.IsEmpty())
                beatmapData.checkpoints.Add(Checkpoint.Default);

            beatmapData.markers = beatmapData.markers.OrderBy(x => x.time).ToList();
            beatmapData.checkpoints = beatmapData.checkpoints.OrderBy(x => x.time).ToList();

            return beatmapData;
        }

        public int GetLastCheckpointIndex()
        {
            var index = checkpoints.FindLastIndex(x => x.time < AudioManager.inst.CurrentAudioSource.time);
            return index < 0 ? 0 : index;
        }

        public Checkpoint GetLastCheckpoint() => checkpoints[GetLastCheckpointIndex()];

        public List<Checkpoint> checkpoints = new List<Checkpoint>();
        public List<Marker> markers = new List<Marker>();

        public LevelData level;
        public LevelEditorData editor;
    }
}
