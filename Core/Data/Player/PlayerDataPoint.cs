using UnityEngine;

using SimpleJSON;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Represents a point where an event occurs on a Player.
    /// </summary>
    public class PlayerDataPoint : PAObject<PlayerDataPoint>
    {
        public PlayerDataPoint() { }

        public PlayerDataPoint(Vector2 position, int checkpointIndex, float time)
        {
            this.position = position;
            this.checkpointIndex = checkpointIndex;
            this.time = time;
        }

        public PlayerDataPoint(Vector2 position) : this(position, GameManager.inst.UpcomingCheckpointIndex, AudioManager.inst.CurrentAudioSource.time) { }

        public PlayerDataPoint(Vector2 position, float time) : this(position, GameManager.inst.UpcomingCheckpointIndex, time) { }

        #region Values

        /// <summary>
        /// Position where the data was triggered.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The active checkpoints' index at the time.
        /// </summary>
        public int checkpointIndex;

        /// <summary>
        /// Time the data was triggered at.
        /// </summary>
        public float time;

        #endregion

        #region Methods

        public override void CopyData(PlayerDataPoint orig, bool newID = true)
        {
            position = orig.position;
            checkpointIndex = orig.checkpointIndex;
            time = orig.time;
        }

        public override void ReadJSON(JSONNode jn)
        {
            position = jn["pos"].AsVector3();
            checkpointIndex = jn["check"].AsInt;
            time = jn["t"].AsFloat;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (position.x != 0f || position.y != 0f)
                jn["pos"] = position.ToJSON();
            if (checkpointIndex != 0)
                jn["check"] = checkpointIndex;
            jn["t"] = time;

            return jn;
        }

        #endregion
    }
}
