namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe that animates a float value.
    /// </summary>
    public struct FloatKeyframe : IKeyframe<float>
    {
        public bool Active { get; set; }

        public float Time { get; set; }
        public EaseFunction Ease { get; set; }
        public float Value { get; set; }

        public FloatKeyframe(float time, float value, EaseFunction ease)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
        }

        public void Start(float time) => Active = true;

        public void Stop() => Active = false;

        public void SetEase(EaseFunction ease) => Ease = ease;

        public void SetValue(float value) => Value = value;

        public float GetValue() => Value;

        public float Interpolate(IKeyframe<float> other, float time) => RTMath.Lerp(GetValue(), other.GetValue(), other.Ease(time));
    }
}
