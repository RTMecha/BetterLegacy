using System.Collections.Generic;

namespace BetterLegacy.Core.Animation
{
    /// <summary>
    /// Indicates the object controls animations.
    /// </summary>
    public interface IAnimationController
    {
        /// <summary>
        /// List of animations.
        /// </summary>
        public List<RTAnimation> Animations { get; set; }

        /// <summary>
        /// Speed the animations should play at.
        /// </summary>
        public float Speed { get; set; }
    }
}
