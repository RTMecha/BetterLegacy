using UnityEngine;

namespace BetterLegacy.Core.Runtime.Objects
{
    /// <summary>
    /// Indicates a runtime object can utilize a Prefab Objects' offsets.
    /// </summary>
    public interface IPrefabOffset
    {
        /// <summary>
        /// Position offset.
        /// </summary>
        public Vector3 PrefabOffsetPosition { get; set; }

        /// <summary>
        /// Scale offset.
        /// </summary>
        public Vector3 PrefabOffsetScale { get; set; }

        /// <summary>
        /// Rotation offset.
        /// </summary>
        public Vector3 PrefabOffsetRotation { get; set; }
    }
}
