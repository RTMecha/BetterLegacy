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

        public void Start();

        public void Stop();

        public void SetEase(EaseFunction ease);

        public T Interpolate(IKeyframe<T> other, float time);
    }
}

