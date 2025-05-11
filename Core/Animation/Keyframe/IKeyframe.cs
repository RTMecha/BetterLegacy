namespace BetterLegacy.Core.Animation.Keyframe
{
    /// <summary>
    /// A keyframe.
    /// </summary>
    /// <typeparam name="T">Output type of the keyframe.</typeparam>
    public interface IKeyframe<T>
    {
        public bool Active { get; set; }

        public float Time { get; set; }

        public EaseFunction Ease { get; set; }

        public void Start(float time);

        public void Stop();

        public void SetEase(EaseFunction ease);

        public void SetValue(T value);

        public T GetValue();

        public T Interpolate(IKeyframe<T> other, float time);
    }
}

