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
        public IKeyframe<float> PreviousKeyframe { get; set; }

        public FloatKeyframe(float time, float value, EaseFunction ease, IKeyframe<float> previousKeyframe = null)
        {
            Time = time;
            Value = value;
            Ease = ease;
            Active = false;
            PreviousKeyframe = previousKeyframe;
        }

        public void Start()
        {

        }

        public void Stop()
        {
            Active = false;
        }

        public float Interpolate(IKeyframe<float> other, float time)
        {
            var value = other is FloatKeyframe vector3Keyframe ? vector3Keyframe.Value : other is DynamicFloatKeyframe dynamicVector3Keyframe ? dynamicVector3Keyframe.Value : other is StaticFloatKeyframe staticVector3Keyframe ? staticVector3Keyframe.Value : 0f;
            var ease = other is FloatKeyframe vector3Keyframe1 ? vector3Keyframe1.Ease(time) : other is DynamicFloatKeyframe dynamicVector3Keyframe1 ? dynamicVector3Keyframe1.Ease(time) : other is StaticFloatKeyframe staticVector3Keyframe1 ? staticVector3Keyframe1.Ease(time) : 0f;

            var prevtarget = PreviousKeyframe != null && PreviousKeyframe is StaticFloatKeyframe ? ((StaticFloatKeyframe)PreviousKeyframe).Angle : 0f;

            return RTMath.Lerp(prevtarget + Value, value, ease);
        }
    }
}
