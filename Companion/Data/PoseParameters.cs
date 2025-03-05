using BetterLegacy.Core.Data;
using System;

using BetterLegacy.Companion.Entity;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Pose parameters passed to <see cref="ExampleModel.SetPose(string, PoseParameters, Action{Core.Animation.RTAnimation})"/>.
    /// </summary>
    public class PoseParameters : Exists
    {
        public PoseParameters() { }

        public PoseParameters(float transitionTime, float speed)
        {
            this.transitionTime = transitionTime;
            this.speed = speed;
        }

        /// <summary>
        /// The default parameters.
        /// </summary>
        public static PoseParameters Default => new PoseParameters();

        /// <summary>
        /// Transition time for the animation.
        /// </summary>
        public float transitionTime = 0.3f;

        /// <summary>
        /// Speed of the animation.
        /// </summary>
        public float speed = 1f;
    }

    // demonstrates custom pose parameters for poses.
    public class TestPoseParameters : PoseParameters
    {
        public TestPoseParameters() : base() { }

        public TestPoseParameters(float transitionTime, float speed, bool toggle) : base(transitionTime, speed)
        {
            this.toggle = toggle;
        }

        public bool toggle;
    }
}
