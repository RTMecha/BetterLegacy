using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Indicates an object is a modifier reference.
    /// </summary>
    public interface IModifierReference
    {
        /// <summary>
        /// The runtime that spawned this object.
        /// </summary>
        public RTLevelBase ParentRuntime { get; set; }

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
