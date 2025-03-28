using System;

using UnityEngine;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Companion.Data.Parameters
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

    public class LookAtPoseParameters : PoseParameters
    {
        public LookAtPoseParameters() : base() { }

        public LookAtPoseParameters(float transitionTime, float speed) : base(transitionTime, speed) { }

        public LookAtPoseParameters(float transitionTime, float speed, bool disableFaceAuto, Vector2 faceLookAt, bool disablePupilsAuto, Vector2 pupilsLookAt) : base(transitionTime, speed)
        {
            this.disableFaceAuto = disableFaceAuto;
            this.faceLookAt = faceLookAt;
            this.disablePupilsAuto = disablePupilsAuto;
            this.pupilsLookAt = pupilsLookAt;
        }

        public bool disableFaceAuto = false;
        public Vector2 faceLookAt;
        public bool disablePupilsAuto = true;
        public Vector2 pupilsLookAt;
    }

    public class RandomPoseParameters : PoseParameters
    {
        public RandomPoseParameters() : base() { }

        public RandomPoseParameters(int poseSelection) : base() => this.poseSelection = () => poseSelection;

        public RandomPoseParameters(Func<int> poseSelection) : base() => this.poseSelection = poseSelection;

        public RandomPoseParameters(int min, int max) : this(UnityEngine.Random.Range(min, max)) { }

        public Func<int> poseSelection;
    }
}
