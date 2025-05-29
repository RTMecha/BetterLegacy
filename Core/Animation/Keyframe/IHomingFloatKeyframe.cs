using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// Indicates a runtime keyframe is a rotation homing keyframe.
    /// </summary>
    public interface IHomingFloatKeyframe
    {
        /// <summary>
        /// Position sequence used to calculate the objects' target angle.
        /// </summary>
        public Sequence<Vector3> PositionSequence { get; set; }

        /// <summary>
        /// Angle the keyframe should turn to.
        /// </summary>
        public float Angle { get; set; }
    }
}
