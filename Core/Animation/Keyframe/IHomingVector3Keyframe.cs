namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// Indicates a runtime keyframe is a position homing keyframe.
    /// </summary>
    public interface IHomingVector3Keyframe
    {
        /// <summary>
        /// Axis to use homing on.
        /// </summary>
        public AxisMode Axis { get; set; }
    }
}
