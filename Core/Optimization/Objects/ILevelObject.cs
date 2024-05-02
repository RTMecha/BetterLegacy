namespace BetterLegacy.Core.Optimization.Objects
{
    /// <summary>
    /// Represents a level object.
    /// </summary>
    public interface ILevelObject
    {
        public float StartTime { get; set; }
        public float KillTime { get; set; }

        public string ID { get; }

        public void SetActive(bool active);

        /// <param name="time">Seconds since the level has started.</param>
        public void Interpolate(float time);
    }
}