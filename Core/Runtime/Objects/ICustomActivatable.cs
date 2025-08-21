namespace BetterLegacy.Core.Runtime.Objects
{
    /// <summary>
    /// Indicates a runtime object can be dynamically enabled / disabled.
    /// </summary>
    public interface ICustomActivatable
    {
        /// <summary>
        /// If the runtime object is currently active. Used for modifiers.
        /// </summary>
        public bool CustomActive { get; set; }

        /// <summary>
        /// Sets the runtime objects' custom active state.
        /// </summary>
        /// <param name="active">Active state.</param>
        public void SetCustomActive(bool active);
    }
}
