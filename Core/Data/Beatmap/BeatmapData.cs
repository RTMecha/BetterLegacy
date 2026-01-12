using System;
using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents the levels' beatmap data. Contains checkpoints, etc.
    /// </summary>
    public class BeatmapData : PAObject<BeatmapData>
    {
        public BeatmapData() { }

        #region Values

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

        #endregion

        #region Functions

        public override void CopyData(BeatmapData orig, bool newID = true)
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
        /// Gets the index of the next checkpoint in time.
        /// </summary>
        /// <returns>Returns the next checkpoints' index.</returns>
        public int GetNextCheckpointIndex()
        {
            var index = checkpoints.FindIndex(x => x.time > AudioManager.inst.CurrentAudioSource.time);
            return index < 0 ? 0 : index;
        }

        /// <summary>
        /// Gets the index of the next checkpoint in time.
        /// </summary>
        /// <param name="predicate">Match predicate.</param>
        /// <returns>Returns the next checkpoints' index.</returns>
        public int GetNextCheckpointIndex(Predicate<Checkpoint> predicate)
        {
            var index = checkpoints.FindIndex(x => x.time > AudioManager.inst.CurrentAudioSource.time && predicate(x));
            return index < 0 ? 0 : index;
        }

        /// <summary>
        /// Gets the next checkpoint in time.
        /// </summary>
        /// <returns>Returns the next checkpoint.</returns>
        public Checkpoint GetNextCheckpoint() => checkpoints.GetAtOrDefault(GetNextCheckpointIndex(), null);

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
        /// Gets the index of the last checkpoint in time.
        /// </summary>
        /// <param name="predicate">Match predicate.</param>
        /// <returns>Returns the last checkpoints' index.</returns>
        public int GetLastCheckpointIndex(Predicate<Checkpoint> predicate)
        {
            var index = checkpoints.FindLastIndex(x => x.time < AudioManager.inst.CurrentAudioSource.time && predicate(x));
            return index < 0 ? 0 : index;
        }

        /// <summary>
        /// Gets the last checkpoint in time.
        /// </summary>
        /// <returns>Returns the last checkpoint.</returns>
        public Checkpoint GetLastCheckpoint() => checkpoints.GetAtOrDefault(GetLastCheckpointIndex(), null);

        /// <summary>
        /// Gets the index of the next marker in time.
        /// </summary>
        /// <returns>Returns the next markers' index.</returns>
        public int GetNextMarkerIndex()
        {
            var index = markers.FindIndex(x => x.time > AudioManager.inst.CurrentAudioSource.time);
            return index < 0 ? 0 : index;
        }

        /// <summary>
        /// Gets the index of the next marker in time.
        /// </summary>
        /// <param name="predicate">Match predicate.</param>
        /// <returns>Returns the next markers' index.</returns>
        public int GetNextMarkerIndex(Predicate<Marker> predicate)
        {
            var index = markers.FindIndex(x => x.time > AudioManager.inst.CurrentAudioSource.time && predicate(x));
            return index < 0 ? 0 : index;
        }

        /// <summary>
        /// Gets the next marker in time.
        /// </summary>
        /// <returns>Returns the next marker.</returns>
        public Marker GetNextMarker() => markers.GetAtOrDefault(GetNextMarkerIndex(), null);

        /// <summary>
        /// Gets the index of the last marker in time.
        /// </summary>
        /// <returns>Returns the last markers' index.</returns>
        public int GetLastMarkerIndex()
        {
            var index = markers.FindLastIndex(x => x.time < AudioManager.inst.CurrentAudioSource.time);
            return index < 0 ? 0 : index;
        }

        /// <summary>
        /// Gets the index of the last marker in time.
        /// </summary>
        /// <param name="predicate">Match predicate.</param>
        /// <returns>Returns the last markers' index.</returns>
        public int GetLastMarkerIndex(Predicate<Marker> predicate)
        {
            var index = markers.FindLastIndex(x => x.time < AudioManager.inst.CurrentAudioSource.time && predicate(x));
            return index;
        }

        /// <summary>
        /// Gets the last marker in time.
        /// </summary>
        /// <returns>Returns the last marker.</returns>
        public Marker GetLastMarker() => markers.GetAtOrDefault(GetLastMarkerIndex(), null);

        #endregion
    }
}
