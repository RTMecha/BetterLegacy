namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a float value.
    /// </summary>
    public struct FloatKeyframe : IKeyframe<float>
    {
        public FloatKeyframe(float time, float value, EaseFunction ease, bool relative = false)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
            TotalValue = 0f;
            Relative = relative;
        }

        #region Values

        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public float Value { get; set; }
        public float TotalValue { get; set; }
        public bool Relative { get; set; }

        #endregion

        #region Methods

        public void Start(IKeyframe<float> prev, float value, float time)
        {
            // get the relative value of the previous homing keyframe
            TotalValue = Relative ? prev is IHomingKeyframe ? prev.GetValue() : prev.TotalValue : 0f;
            Active = true;
        }

        public void Stop() => Active = false;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(float value) => Value = value;

        public float GetValue() => Value + TotalValue;

        public float Interpolate(IKeyframe<float> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));

        #endregion
    }
}
