namespace BetterLegacy.Core.Runtime.Events
{
    /// <summary>
    /// Represents a value to offset to an event.
    /// </summary>
    public class EventOffset
    {
        #region Constructors

        public EventOffset() { }

        public EventOffset(float value) => this.value = value;

        #endregion

        #region Values

        /// <summary>
        /// Value to offset.
        /// </summary>
        public float value;

        /// <summary>
        /// Operation of the offset.
        /// </summary>
        public MathOperation operation = MathOperation.Addition;

        #endregion

        #region Operators

        public static implicit operator float(EventOffset eventOffset) => eventOffset.value;

        public static implicit operator EventOffset(float value) => new EventOffset(value);

        #endregion
    }
}
