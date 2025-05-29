namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// Indicates a runtime keyframe is a dynamic homing keyframe, capable of constantly tracking the player.
    /// </summary>
    /// <typeparam name="T">Output type of the keyframe.</typeparam>
    public interface IDynamicHomingKeyframe<T>
    {
        #region Values

        public T OriginalValue { get; set; }

        /// <summary>
        /// Minimum range to track. If the player enters this range, the keyframe stops tracking.
        /// </summary>
        public float MinRange { get; set; }

        /// <summary>
        /// Maximum range to track. If the player exits this range, the keyframe stops tracking.
        /// </summary>
        public float MaxRange { get; set; }

        /// <summary>
        /// Delay of the dynamic homing. If the value is 0, the homing has no delay. If the value is 1, the homing stops moving.
        /// </summary>
        public float Delay { get; set; }

        /// <summary>
        /// If the homing keyframe should flee from the player rather than follow.
        /// </summary>
        public bool Flee { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the value that keyframe interpolation should use.
        /// </summary>
        /// <param name="ease">Eased time scale.</param>
        /// <returns>Returns the value of the keyframe.</returns>
        public T GetValue(float ease);

        /// <summary>
        /// Gets the value of the dynamic homing keyframe. Interpolates only min & max range and delay.
        /// </summary>
        /// <param name="dynamicHomingKeyframe">Next dynamic homing keyframe. If is null, doesn't interpolate.</param>
        /// <param name="ease">Eased time scale.</param>
        /// <returns>Returns the dynamic homing value.</returns>
        public T GetValue(IDynamicHomingKeyframe<T> dynamicHomingKeyframe, float ease);

        /// <summary>
        /// Calculates the current delay.
        /// </summary>
        /// <returns>Returns the calculated delay.</returns>
        public float CalculateDelay();

        #endregion
    }
}
