namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Indicates an object is selectable.
    /// </summary>
    public interface ISelectable
    {
        /// <summary>
        /// If the object is currently selected.
        /// </summary>
        public bool Selected { get; set; }
    }
}
