using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using UnityEngine;

using BetterLegacy.Core.Animation.Keyframe;

namespace BetterLegacy.Core.Animation
{
    /// <summary>
    /// Sequence class. Stores, manages and interpolates between keyframes.
    /// </summary>
    public class Sequence<T>
    {
        public readonly IKeyframe<T>[] keyframes;

        public T Value { get; set; }
        public float Time { get; set; }

        public Sequence(IEnumerable<IKeyframe<T>> keyframes)
        {
            this.keyframes = keyframes.ToArray();
            Array.Sort(this.keyframes, (x, y) => x.Time.CompareTo(y.Time));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Interpolate(float time)
        {
            if (keyframes.Length == 0)
                throw new NoKeyframeException("Cannot interpolate in an empty sequence!");

            Time = time;

            var first = keyframes[0];
            if (keyframes.Length == 1 || time < first.Time)
            {
                if (!first.Active)
                {
                    first.Active = true;
                    first.Start();
                }

                Value = ResultFromSingleKeyframe(first, 1f);
                return Value;
            }

            var last = keyframes[keyframes.Length - 1];
            if (time >= last.Time)
            {
                if (!last.Active)
                {
                    last.Active = true;
                    last.Start();
                }

                Value = ResultFromSingleKeyframe(keyframes[keyframes.Length - 1], 0.0f);
                return Value;
            }

            int index = Search(time);
            IKeyframe<T> current = keyframes[index];
            IKeyframe<T> next = keyframes[index + 1];

            if (!current.Active)
            {
                current.Active = true;
                current.Start();
            }

            float t = Mathf.InverseLerp(current.Time, next.Time, time);
            Value = current.Interpolate(next, t);
            return Value;
        }

        // Binary search for the keyframe pair that contains the given time
        int Search(float time)
        {
            int low = 0;
            int high = keyframes.Length - 1;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                float midTime = keyframes[mid].Time;

                if (time < midTime)
                    high = mid - 1;
                else if (time > midTime)
                    low = mid + 1;
                else
                    return mid;
            }

            return low - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T ResultFromSingleKeyframe(IKeyframe<T> keyframe, float t) => keyframe.Interpolate(keyframe, t);
    }
}
