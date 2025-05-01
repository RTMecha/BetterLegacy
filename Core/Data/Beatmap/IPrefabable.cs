using BetterLegacy.Core.Runtime.Objects;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Indicates an object can be stored in a <see cref="Prefab"/> and spawned from a <see cref="PrefabObject"/>.
    /// </summary>
    public interface IPrefabable
    {
        /// <summary>
        /// Original ID of the object from the prefab.
        /// </summary>
        public string OriginalID { get; set; }

        /// <summary>
        /// Prefab reference ID.
        /// </summary>
        public string PrefabID { get; set; }

        /// <summary>
        /// Prefab Object reference ID.
        /// </summary>
        public string PrefabInstanceID { get; set; }

        /// <summary>
        /// If the object was spawned from a prefab.
        /// </summary>
        public bool FromPrefab { get; set; }

        /// <summary>
        /// Gets the runtime object for the prefabbed object.
        /// </summary>
        /// <returns>Returns the runtime object of the object.</returns>
        public IRTObject GetRuntimeObject();
    }
}
