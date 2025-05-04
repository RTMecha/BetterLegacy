namespace BetterLegacy.Core.Runtime.Objects
{
    /// <summary>
    /// Represents a Runtime Object.
    /// </summary>
    public interface IRTObject
    {
        /// <summary>
        /// Time the object spawns at.
        /// </summary>
        public float StartTime { get; set; }

        /// <summary>
        /// Time the object despawns at.
        /// </summary>
        public float KillTime { get; set; }

        /// <summary>
        /// Room number the object renders under. If the level's room number is 0, then this is ignored.
        /// </summary>
        public int Room { get; set; }

        /// <summary>
        /// Clears the data of the object.
        /// </summary>
        public void Clear();

        /// <summary>
        /// Sets the active state of the object.
        /// </summary>
        /// <param name="active">Active state.</param>
        public void SetActive(bool active);

        /// <summary>
        /// Interpolates the objects' animations.
        /// </summary>
        /// <param name="time">Seconds since the level has started.</param>
        public void Interpolate(float time);
    }
}