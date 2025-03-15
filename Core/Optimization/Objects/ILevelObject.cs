namespace BetterLegacy.Core.Optimization.Objects
{
    /// <summary>
    /// Represents a level object.
    /// </summary>
    public interface ILevelObject
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
        /// Sets the active state of the object.
        /// </summary>
        /// <param name="active"></param>
        public void SetActive(bool active);

        /// <summary>
        /// Interpolates the objects' animations.
        /// </summary>
        /// <param name="time">Seconds since the level has started.</param>
        public void Interpolate(float time);
    }
}