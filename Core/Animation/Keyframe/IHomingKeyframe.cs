using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// Indicates a runtime keyframe is a homing keyframe, meaning a keyframe that can track the players' position.
    /// </summary>
    public interface IHomingKeyframe
    {
        /// <summary>
        /// Tracked player position the keyframe will home on.
        /// </summary>
        public Vector3 Target { get; set; }

        /// <summary>
        /// Player target priority.
        /// </summary>
        public HomingPriority Priority { get; set; }

        /// <summary>
        /// Index of the player if <see cref="Priority"/> is set to <see cref="HomingPriority.Index"/>.
        /// </summary>
        public int PlayerIndex { get; set; }

        /// <summary>
        /// Gets the position of the current object and uses it to target the closest player.
        /// </summary>
        /// <returns>Returns the position of the current object.</returns>
        public Vector3 GetPosition();

        /// <summary>
        /// Interpolates the position of the current object and uses it to target the closest player.
        /// </summary>
        /// <param name="time">Time value to interpolate.</param>
        /// <returns>Returns the interpolated position of the current object.</returns>
        public Vector3 GetPosition(float time);
    }
}
