using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class LevelBeatmapData : PAObject<LevelBeatmapData>
    {
        public LevelBeatmapData() { }

        public override void CopyData(LevelBeatmapData orig, bool newID = true)
        {
            checkpoints = orig.checkpoints.Select(x => x.Copy()).ToList();
            markers = orig.markers.Select(x => x.Copy()).ToList();

            VerifyData();

            level.CopyData(orig.level);
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            VerifyData();

            markers.Clear();
            for (int i = 0; i < jn["markers"].Count; i++)
                markers.Add(Marker.ParseVG(jn["markers"][i], version));

            checkpoints.Clear();
            for (int i = 0; i < jn["checkpoints"].Count; i++)
                checkpoints.Add(Checkpoint.ParseVG(jn["checkpoints"][i], version));
            ValidateCheckpoints();

            markers = markers.OrderBy(x => x.time).ToList();
            checkpoints = checkpoints.OrderBy(x => x.time).ToList();
        }

        public override void ReadJSON(JSONNode jn)
        {
            VerifyData();

            level.ReadJSON(jn["level_data"]);

            for (int i = 0; i < jn["ed"]["markers"].Count; i++)
                markers.Add(Marker.Parse(jn["ed"]["markers"][i]));

            for (int i = 0; i < jn["checkpoints"].Count; i++)
                checkpoints.Add(Checkpoint.Parse(jn["checkpoints"][i]));
            ValidateCheckpoints();

            markers = markers.OrderBy(x => x.time).ToList();
            checkpoints = checkpoints.OrderBy(x => x.time).ToList();

        }

        /// <summary>
        /// Verifies the inner data.
        /// </summary>
        public void VerifyData()
        {
            if (!level)
                level = new LevelData();
        }

        /// <summary>
        /// If the checkpoints list is empty, adds the default checkpoint.
        /// </summary>
        public void ValidateCheckpoints()
        {
            if (checkpoints.IsEmpty())
                checkpoints.Add(Checkpoint.Default);
        }

        /// <summary>
        /// Gets the index of the last checkpoint in time.
        /// </summary>
        /// <returns>Returns the last checkpoints' index.</returns>
        public int GetLastCheckpointIndex()
        {
            var index = checkpoints.FindLastIndex(x => x.time < AudioManager.inst.CurrentAudioSource.time);
            return index < 0 ? 0 : index;
        }

        /// <summary>
        /// Gets the last checkpoint in time.
        /// </summary>
        /// <returns>Returns the last checkpoint.</returns>
        public Checkpoint GetLastCheckpoint() => checkpoints[GetLastCheckpointIndex()];

        /// <summary>
        /// List of checkpoints.
        /// </summary>
        public List<Checkpoint> checkpoints = new List<Checkpoint>();

        /// <summary>
        /// List of markers. This would be located in an editor related object, but unfortunately that's not how it works in vanilla..... .-.
        /// </summary>
        public List<Marker> markers = new List<Marker>();

        /// <summary>
        /// Level data that controls general behavior of the level.
        /// </summary>
        public LevelData level;
    }
}
