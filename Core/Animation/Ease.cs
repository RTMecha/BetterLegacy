using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterLegacy.Core.Animation
{
    public delegate float EaseFunction(float t);

    /// <summary>
    /// Static class with useful easer functions that can be used by Tweens.
    /// </summary>
    public static class Ease
    {
        static readonly Dictionary<string, EaseFunction> EaseLookup = new Dictionary<string, EaseFunction>()
        {
            { "Linear", Linear },
            { "Instant", Instant },
            { "InSine", SineIn },
            { "OutSine", SineOut },
            { "InOutSine", SineInOut },
            { "InElastic", ElasticIn },
            { "OutElastic", ElasticOut },
            { "InOutElastic", ElasticInOut },
            { "InBack", BackIn },
            { "OutBack", BackOut },
            { "InOutBack", BackInOut },
            { "InBounce", BounceIn },
            { "OutBounce", BounceOut },
            { "InOutBounce", BounceInOut },
            { "InQuad", QuadIn },
            { "OutQuad", QuadOut },
            { "InOutQuad", QuadInOut },
            { "InCirc", CircIn },
            { "OutCirc", CircOut },
            { "InOutCirc", CircInOut },
            { "InExpo", ExpoIn },
            { "OutExpo", ExpoOut },
            { "InOutExpo", ExpoInOut }
        };

        public static EaseFunction GetEaseFunction(string name) => EaseLookup[name];
        public static bool HasEaseFunction(string name) => EaseLookup.ContainsKey(name);

        public static bool TryGetEaseFunction(string name, out EaseFunction easeFunction) => EaseLookup.TryGetValue(name, out easeFunction);

        public static EaseFunction GetEaseFunction(string name, EaseFunction defaultEase) => TryGetEaseFunction(name, out EaseFunction easeFunction) ? easeFunction : defaultEase;

        const float PI = 3.14159265359f;
        const float PI2 = PI / 2;
        const float B1 = 1 / 2.75f;
        const float B2 = 2 / 2.75f;
        const float B3 = 1.5f / 2.75f;
        const float B4 = 2.5f / 2.75f;
        const float B5 = 2.25f / 2.75f;
        const float B6 = 2.625f / 2.75f;

        /// <summary>
        /// Ease a value to its target and then back. Use this to wrap another easing function.
        /// </summary>
        public static Func<float, float> ToAndFro(EaseFunction easer) => t => ToAndFro(easer(t));

        /// <summary>
        /// Ease a value to its target and then back.
        /// </summary>
        public static float ToAndFro(float t) => t < 0.5f ? t * 2 : 1 + ((t - 0.5f) / 0.5f) * -1;

        /// <summary>
        /// Linear.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float Linear(float t) => t;

        public static float Loutear(float t) => -t + 1f;

        /// <summary>
        /// Instant.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float Instant(float t) => 0.0f;

        /// <summary>
        /// lol
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float Outstant(float t) => 1.0f;

        public static float InterpolateEase(float t, float ease) => ease == 0.0f ? t :
            ease > 0f && ease <= 1f ? RTMath.Lerp(Linear(t), QuadInOut(t), ease) :
            ease > 1f && ease <= 2f ? RTMath.Lerp(QuadInOut(t), CubicInOut(t), ease - 1f) :
            ease > 2f && ease <= 3f ? RTMath.Lerp(CubicInOut(t), QuartInOut(t), ease - 2f) :
            ease > 3f && ease <= 4f ? RTMath.Lerp(QuartInOut(t), QuintInOut(t), ease - 3f) : 1f;

        #region Sine

        /// <summary>
        /// Sine in.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float SineIn(float t) => t == 1 ? 1 : -Mathf.Cos(PI2 * t) + 1;

        /// <summary>
        /// Sine out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float SineOut(float t) => Mathf.Sin(PI2 * t);

        /// <summary>
        /// Sine in and out
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float SineInOut(float t) => -Mathf.Cos(PI * t) / 2 + 0.5f;

        #endregion

        #region Elastic

        /// <summary>
        /// Elastic in.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float ElasticIn(float t) => Mathf.Sin(13 * PI2 * t) * Mathf.Pow(2, 10 * (t - 1));

        /// <summary>
        /// Elastic out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float ElasticOut(float t) => t == 1 ? 1 : (Mathf.Sin(-13 * PI2 * (t + 1)) * Mathf.Pow(2, -10 * t) + 1);

        /// <summary>
        /// Elastic in and out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float ElasticInOut(float t)
            => t < 0.5 ? (0.5f * Mathf.Sin(13 * PI2 * (2 * t)) * Mathf.Pow(2, 10 * ((2 * t) - 1)))
            : (0.5f * (Mathf.Sin(-13 * PI2 * ((2 * t - 1) + 1)) * Mathf.Pow(2, -10 * (2 * t - 1)) + 2));
        //{
        //    if (t < 0.5)
        //    {
        //        return (0.5f * Mathf.Sin(13 * PI2 * (2 * t)) * Mathf.Pow(2, 10 * ((2 * t) - 1)));
        //    }

        //    return (0.5f * (Mathf.Sin(-13 * PI2 * ((2 * t - 1) + 1)) * Mathf.Pow(2, -10 * (2 * t - 1)) + 2));
        //}

        #endregion

        #region Back

        /// <summary>
        /// Back in.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float BackIn(float t) => t * t * (2.70158f * t - 1.70158f);

        /// <summary>
        /// Back out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float BackOut(float t) => 1 - (--t) * (t) * (-2.70158f * t - 1.70158f);

        /// <summary>
        /// Back in and out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float BackInOut(float t)
        {
            t *= 2;
            if (t < 1) return (t * t * (2.70158f * t - 1.70158f) / 2);
            t--;
            return ((1 - (--t) * (t) * (-2.70158f * t - 1.70158f)) / 2 + .5f);
        }

        #endregion

        #region Bounce

        /// <summary>
        /// Bounce in.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float BounceIn(float t)
        {
            t = 1 - t;
            if (t < B1) return (1 - 7.5625f * t * t);
            if (t < B2) return (1 - (7.5625f * (t - B3) * (t - B3) + .75f));
            if (t < B4) return (1 - (7.5625f * (t - B5) * (t - B5) + .9375f));
            return (1 - (7.5625f * (t - B6) * (t - B6) + .984375f));
        }

        /// <summary>
        /// Bounce out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float BounceOut(float t)
        {
            if (t < B1) return (7.5625f * t * t);
            if (t < B2) return (7.5625f * (t - B3) * (t - B3) + .75f);
            if (t < B4) return (7.5625f * (t - B5) * (t - B5) + .9375f);
            return (7.5625f * (t - B6) * (t - B6) + .984375f);
        }

        /// <summary>
        /// Bounce in and out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float BounceInOut(float t)
        {
            if (t < .5)
            {
                t = 1 - t * 2;
                if (t < B1) return ((1 - 7.5625f * t * t) / 2);
                if (t < B2) return ((1 - (7.5625f * (t - B3) * (t - B3) + .75f)) / 2);
                if (t < B4) return ((1 - (7.5625f * (t - B5) * (t - B5) + .9375f)) / 2);
                return ((1 - (7.5625f * (t - B6) * (t - B6) + .984375f)) / 2);
            }

            t = t * 2 - 1;
            if (t < B1) return ((7.5625f * t * t) / 2 + .5f);
            if (t < B2) return ((7.5625f * (t - B3) * (t - B3) + .75f) / 2 + .5f);
            if (t < B4) return ((7.5625f * (t - B5) * (t - B5) + .9375f) / 2 + .5f);
            return ((7.5625f * (t - B6) * (t - B6) + .984375f) / 2 + .5f);
        }

        #endregion

        #region Quad

        /// <summary>
        /// Quadratic in.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float QuadIn(float t) => t * t;

        /// <summary>
        /// Quadratic out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float QuadOut(float t) => -t * (t - 2);

        /// <summary>
        /// Quadratic in and out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float QuadInOut(float t) => t <= .5 ? t * t * 2 : 1 - (--t) * t * 2;

        #endregion

        #region Circ

        /// <summary>
        /// Circle in.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float CircIn(float t) => -(Mathf.Sqrt(1 - t * t) - 1);

        /// <summary>
        /// Circle out
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float CircOut(float t) => Mathf.Sqrt(1 - (t - 1) * (t - 1));

        /// <summary>
        /// Circle in and out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float CircInOut(float t) => t <= .5 ? (Mathf.Sqrt(1 - t * t * 4) - 1) / -2 : (Mathf.Sqrt(1 - (t * 2 - 2) * (t * 2 - 2)) + 1) / 2;

        #endregion

        #region Expo

        /// <summary>
        /// Exponential in.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float ExpoIn(float t) => Mathf.Pow(2, 10 * (t - 1));

        /// <summary>
        /// Exponential out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float ExpoOut(float t) => t == 1 ? 1 : (-Mathf.Pow(2, -10 * t) + 1);

        /// <summary>
        /// Exponential in and out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float ExpoInOut(float t) => t == 1 ? 1 : (t < .5 ? Mathf.Pow(2, 10 * (t * 2 - 1)) / 2 : (-Mathf.Pow(2, -10 * (t * 2 - 1)) + 2) / 2);

        #endregion

        #region Cubic

        /// <summary>
        /// Cubic in.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float CubicIn(float t) => t * t * t;

        /// <summary>
        /// Cubic out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float CubicOut(float t) => -t * (t - 3);

        /// <summary>
        /// Cubic in and out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float CubicInOut(float t) => t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;

        #endregion

        #region Quart

        /// <summary>
        /// Quart in.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float QuartIn(float t) => t * t * t * t;

        /// <summary>
        /// Quart out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float QuartOut(float t) => -t * (t - 4);

        /// <summary>
        /// Quart in and out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float QuartInOut(float t) => t < 0.5f ? 8f * t * t * t * t : 1 - Mathf.Pow(-2f * t + 2, 4) / 2;

        #endregion

        #region Quint

        /// <summary>
        /// Quint in.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float QuintIn(float t) => t * t * t * t * t;

        /// <summary>
        /// Quint out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float QuintOut(float t) => -t * (t - 5);

        /// <summary>
        /// Quint in and out.
        /// </summary>
        /// <param name="t">Time elapsed.</param>
        /// <returns>Eased timescale.</returns>
        public static float QuintInOut(float t) => t < 0.5f ? 16f * t * t * t * t * t : 1 - Mathf.Pow(-2f * t + 2f, 5) / 2f;

        #endregion
    }
}
