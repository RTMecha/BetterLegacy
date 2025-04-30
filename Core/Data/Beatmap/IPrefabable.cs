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
        /// Prefab reference ID.
        /// </summary>
        public string PrefabID { get; set; }

        /// <summary>
        /// Prefab Object reference ID.
        /// </summary>
        public string PrefabInstanceID { get; set; }

        /// <summary>
        /// Sets the Prefab and Prefab Object ID references from a prefabable.
        /// </summary>
        /// <param name="prefabable">Prefabable object reference.</param>
        public void SetPrefabReference(IPrefabable prefabable);

        /// <summary>
        /// Removes the prefab references.
        /// </summary>
        public void RemovePrefabReference();
    }
}
