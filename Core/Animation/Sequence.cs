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
            //for (int i = 0; i < keyframes.Length; i++)
            //    if (time < keyframes[i].Time)
            //        keyframes[i].Active = false;

            if (keyframes.Length == 1 || time < keyframes[0].Time)
            {
                if (!keyframes[0].Active)
                {
                    keyframes[0].Active = true;
                    keyframes[0].Start();
                }

                Value = ResultFromSingleKeyframe(keyframes[0], 1f);
                return Value;
            }

            if (time >= keyframes[keyframes.Length - 1].Time)
            {
                if (!keyframes[keyframes.Length - 1].Active)
                {
                    keyframes[keyframes.Length - 1].Active = true;
                    keyframes[keyframes.Length - 1].Start();
                }
                Value = ResultFromSingleKeyframe(keyframes[keyframes.Length - 1], 0.0f);
                return Value;
            }

            int index = Search(time);
            IKeyframe<T> first = keyframes[index];
            IKeyframe<T> second = keyframes[index + 1];

            if (!first.Active)
            {
                first.Active = true;
                first.Start();
            }

            float t = Mathf.InverseLerp(first.Time, second.Time, time);
            Value = first.Interpolate(second, t);
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
