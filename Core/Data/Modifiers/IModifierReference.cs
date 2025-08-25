using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;

namespace BetterLegacy.Core.Data.Modifiers
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

        /// <summary>
        /// Gets the runtime object for the prefabbed object.
        /// </summary>
        /// <returns>Returns the runtime object of the object.</returns>
        public IRTObject GetRuntimeObject();

        /// <summary>
        /// Get the object as a prefabable.
        /// </summary>
        /// <returns>Returns the object as a prefabable.</returns>
        public IPrefabable AsPrefabable();

        /// <summary>
        /// Get the object as a transformable.
        /// </summary>
        /// <returns>Returns the object as a transformable.</returns>
        public ITransformable AsTransformable();
    }
}
