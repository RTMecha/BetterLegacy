using BetterLegacy.Companion.Entity;
using BetterLegacy.Core.Animation;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Represents a dynamic pose that can be applied to the model.
    /// </summary>
    public class ExamplePose
    {
        public ExamplePose(string name, GetAnimation get)
        {
            this.name = name;
            this.get = get;
        }

        /// <summary>
        /// Registered name of the pose.
        /// </summary>
        public string name;

        /// <summary>
        /// Gets the animation.
        /// </summary>
        public GetAnimation get;

        /// <summary>
        /// Delegate for generating a new animation.
        /// </summary>
        /// <param name="model">Passed model.</param>
        /// <returns>Returns a new animation.</returns>
        public delegate RTAnimation GetAnimation(ExampleModel model, PoseParameters parameters);
    }

}
