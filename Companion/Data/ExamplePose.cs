using BetterLegacy.Companion.Entity;
using BetterLegacy.Core.Animation;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Represents a dynamic pose that can be applied to the model.
    /// </summary>
    public class ExamplePose
    {
        public ExamplePose(string key, GetAnimation get)
        {
            this.key = key;
            this.get = get;
        }

        /// <summary>
        /// Registered key of the pose.
        /// </summary>
        public string key;

        /// <summary>
        /// Gets the animation.
        /// </summary>
        public GetAnimation get;

        /// <summary>
        /// Delegate for generating a new animation.
        /// </summary>
        /// <param name="model">Passed model.</param>
        /// <param name="parameters">Passed parameters.</param>
        /// <returns>Returns a new animation.</returns>
        public delegate RTAnimation GetAnimation(ExampleModel model, PoseParameters parameters);
    }

}
