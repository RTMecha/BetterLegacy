namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Indicates an object has a spawn time and a despawn time.
    /// </summary>
    /// <typeparam name="AKT">Autokill Type.</typeparam>
    public interface ILifetime<AKT> where AKT : struct
    {
        /// <summary>
        /// Object spawn time.
        /// </summary>
        public float StartTime { get; set; }

        /// <summary>
        /// Object despawn behavior.
        /// </summary>
        public AKT AutoKillType { get; set; }

        /// <summary>
        /// Autokill time offset.
        /// </summary>
        public float AutoKillOffset { get; set; }

        /// <summary>
        /// Gets if the current audio time is within the lifespan of the object.
        /// </summary>
        public bool Alive { get; }

        /// <summary>
        /// Gets the total time the object is alive for.
        /// </summary>
        public float SpawnDuration { get; }

        /// <summary>
        /// Gets the objects' lifetime based on its autokill type and offset.
        /// </summary>
        /// <param name="offset">Offset to apply to lifetime.</param>
        /// <param name="noAutokill">If the autokill length should be considered.</param>
        /// <param name="collapse">If the length should be collapsed.</param>
        /// <returns>Returns the lifetime of the object.</returns>
        public float GetObjectLifeLength(float offset = 0f, bool noAutokill = false, bool collapse = false);
    }
}
