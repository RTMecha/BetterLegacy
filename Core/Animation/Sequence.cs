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
        public int currentIndex = 0;
        float prevTime = 0.0f;
        public float Time { get; set; }

        IKeyframe<T> prevKeyframe;
        IKeyframe<T> nextKeyframe;

        public Sequence(IEnumerable<IKeyframe<T>> keyframes)
        {
            this.keyframes = keyframes.ToArray();
            Array.Sort(this.keyframes, (x, y) => x.Time.CompareTo(y.Time));
            prevKeyframe = this.keyframes.GetAtOrDefault(0, null);
            nextKeyframe = this.keyframes.GetAtOrDefault(1, prevKeyframe);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue(float t) => t == Time ? Value : Interpolate(t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Interpolate(float time)
        {
            if (keyframes.Length == 0)
                throw new NoKeyframeException("Cannot interpolate in an empty sequence!");

            Time = time;

            if (time >= prevTime)
                UpdateKeyframesForward(time);
            else
                UpdateKeyframesBackward(time);

            if (prevKeyframe == null)
                throw new NoKeyframeException("Cannot interpolate without a previous and next keyframe.");

            if (!nextKeyframe.Active)
            {
                nextKeyframe.Active = true;
                nextKeyframe.Start(prevKeyframe, Value, time);
            }

            float t = prevKeyframe.Time == nextKeyframe.Time ? currentIndex == 0 ? 1f : 0f : Mathf.InverseLerp(prevKeyframe.Time, nextKeyframe.Time, time);
            Value = prevKeyframe.Interpolate(nextKeyframe, t);
            prevTime = time;
            return Value;
        }

        void UpdateKeyframesForward(float time)
        {
            while (keyframes.TryGetAt(currentIndex, out IKeyframe<T> keyframe) && time >= keyframe.Time)
            {
                prevKeyframe = keyframe;
                nextKeyframe = keyframes.GetAtOrDefault(currentIndex + 1, prevKeyframe);
                currentIndex++;
            }
        }

        void UpdateKeyframesBackward(float time)
        {
            while (keyframes.TryGetAt(currentIndex - 1, out IKeyframe<T> keyframe) && time < keyframe.Time)
            {
                nextKeyframe = keyframe;
                prevKeyframe = keyframes.GetAtOrDefault(currentIndex - 2, nextKeyframe);
                currentIndex--;
            }
        }

        void Validate(float time)
        {
            if (prevKeyframe != null && nextKeyframe != null)
                return;

            Helpers.CoreHelper.Log($"Had to validate keyframes.");

            var first = keyframes[0];
            if (keyframes.Length == 1 || time < first.Time)
            {
                prevKeyframe = first;
                nextKeyframe = keyframes.GetAtOrDefault(1, prevKeyframe);
                currentIndex = 0;
                return;
            }

            var last = keyframes[keyframes.Length - 1];
            if (time >= last.Time)
            {
                prevKeyframe = last;
                nextKeyframe = last;
                currentIndex = keyframes.Length - 1;
                return;
            }

            var index = Search(time);
            prevKeyframe = keyframes[index];
            nextKeyframe = keyframes.GetAtOrDefault(index + 1, prevKeyframe);
            currentIndex = index;
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

        T OriginalInterpolate(float time)
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
                    first.Start(first, Value, time);
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
                    last.Start(last, Value, time);
                }

                Value = ResultFromSingleKeyframe(keyframes[keyframes.Length - 1], 0.0f);
                return Value;
            }

            int index = Search(time);
            IKeyframe<T> current = keyframes[index];
            IKeyframe<T> next = keyframes[index + 1];

            if (!next.Active)
            {
                next.Active = true;
                next.Start(current, Value, time);
            }

            float t = Mathf.InverseLerp(current.Time, next.Time, time);
            Value = current.Interpolate(next, t);
            return Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T ResultFromSingleKeyframe(IKeyframe<T> keyframe, float t) => keyframe.Interpolate(keyframe, t);
    }
}
