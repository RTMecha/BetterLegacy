namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// How not found attributes should be handled.
    /// </summary>
    public enum ExampleAttributeRetrieval
    {
        /// <summary>
        /// Throws a <see cref="NullReferenceException"/>.
        /// </summary>
        Throw,
        /// <summary>
        /// Adds a new attribute.
        /// </summary>
        Add,
        /// <summary>
        /// Returns a null reference.
        /// </summary>
        Nothing,
    }
}
