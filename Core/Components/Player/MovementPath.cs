using UnityEngine;

namespace BetterLegacy.Core.Components.Player
{
    /// <summary>
    /// Represents the path of the Legacy tail.
    /// </summary>
    public class MovementPath
    {
        #region Constructors

        public MovementPath(Vector3 pos, Quaternion rot, Transform transform)
        {
            this.pos = pos;
            this.rot = rot;
            this.transform = transform;
            lastPos = pos;
        }

        public MovementPath(Vector3 pos, Quaternion rot, Transform transform, bool active)
        {
            this.pos = pos;
            this.rot = rot;
            this.transform = transform;
            this.active = active;
            lastPos = pos;
        }

        #endregion

        #region Values

        /// <summary>
        /// If the movement path node is active. If this is false, then nodes that come after this one should replace this node's position and rotation.
        /// </summary>
        public bool active = true;

        /// <summary>
        /// Last position of the movement node.
        /// </summary>
        public Vector3 lastPos;

        /// <summary>
        /// Current position of the movement node.
        /// </summary>
        public Vector3 pos;

        /// <summary>
        /// Current rotation of the movement node.
        /// </summary>
        public Quaternion rot;

        /// <summary>
        /// Transform reference to apply the movement node to.
        /// </summary>
        public Transform transform;

        #endregion
    }
}
