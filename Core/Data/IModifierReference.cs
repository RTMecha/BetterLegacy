namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Indicates an object is a modifier reference.
    /// </summary>
    public interface IModifierReference
    {
        /// <summary>
        /// The reference type of the modifyable.
        /// </summary>
        public ModifierReferenceType ReferenceType { get; }

        /// <summary>
        /// Variable set and used by modifiers.
        /// </summary>
        public int IntVariable { get; set; }
    }
}
