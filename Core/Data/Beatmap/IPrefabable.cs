using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Sets the Prefab and Prefab Object ID references from a Prefab Object.
        /// </summary>
        /// <param name="prefabObject">Prefab Object reference.</param>
        public void SetPrefabReference(PrefabObject prefabObject);

        /// <summary>
        /// Sets the Prefab and Prefab Object ID references from a prefabable.
        /// </summary>
        /// <param name="prefabable">Prefabable object reference.</param>
        public void SetPrefabReference(IPrefabable prefabable);

        /// <summary>
        /// Removes the prefab references.
        /// </summary>
        public void RemovePrefabReference();

        /// <summary>
        /// Gets the prefab reference.
        /// </summary>
        public Prefab GetPrefab();

        /// <summary>
        /// Tries to get the Prefab Object reference.
        /// </summary>
        /// <param name="result">Output object.</param>
        /// <returns>Returns true if a Prefab Object was found, otherwise returns false.</returns>
        public bool TryGetPrefabObject(out PrefabObject result);

        /// <summary>
        /// Gets the prefab object reference.
        /// </summary>
        public PrefabObject GetPrefabObject();

    }
}
