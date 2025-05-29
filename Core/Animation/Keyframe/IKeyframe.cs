namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A runtime keyframe.
    /// </summary>
    /// <typeparam name="T">Output type of the keyframe.</typeparam>
    public interface IKeyframe<T>
    {
        #region Values

        /// <summary>
        /// If the keyframe started interpolating.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Time of the keyframe.
        /// </summary>
        public float Time { get; set; }

        /// <summary>
        /// Ease function.
        /// </summary>
        public EaseFunction Ease { get; set; }

        /// <summary>
        /// The value of the keyframe.
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// The total value of the sequence.
        /// </summary>
        public T TotalValue { get; set; }

        /// <summary>
        /// If the keyframe is relative / additive to previous keyframes in the sequence.
        /// </summary>
        public bool Relative { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Starts the keyframe's interpolation to the next keyframe. Used for caching specific values.
        /// </summary>
        /// <param name="prev">The previous runtime keyframe.</param>
        /// <param name="value">The current value of the sequence the keyframe is in.</param>
        /// <param name="time">The time the sequence is currently interpolating to.</param>
        public void Start(IKeyframe<T> prev, T value, float time);

        /// <summary>
        /// Stops the keyframes' interpolation. Used for resetting the cached values.
        /// </summary>
        public void Stop();

        /// <summary>
        /// Sets the keyframes' ease.
        /// </summary>
        /// <param name="ease">Ease function.</param>
        public void SetEase(EaseFunction ease);

        /// <summary>
        /// Sets the keyframes' value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        public void SetValue(T value);

        /// <summary>
        /// Gets the value that keyframe interpolation should use.
        /// </summary>
        /// <returns>Returns the value of the keyframe.</returns>
        public T GetValue();

        /// <summary>
        /// Interpolates between keyframes.
        /// </summary>
        /// <param name="other">The next runtime keyframe to interpolate to.</param>
        /// <param name="time">Time scale.</param>
        /// <returns>Returns the interpolated value.</returns>
        public T Interpolate(IKeyframe<T> other, float time);

        #endregion
    }
}

