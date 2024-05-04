using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterLegacy.Core
{
    public static class RTMath
    {
        public static float Lerp(float x, float y, float t) => x + (y - x) * t;
        public static Vector2 Lerp(Vector2 x, Vector2 y, float t) => x + (y - x) * t;
        public static Vector3 Lerp(Vector3 x, Vector3 y, float t) => x + (y - x) * t;
        public static Color Lerp(Color x, Color y, float t) => x + (y - x) * t;

        public static bool IsNaNInfinity(float f) => float.IsNaN(f) || float.IsInfinity(f);

        public static float Clamp(float value, float min, float max) => Mathf.Clamp(value, min, max);
        public static int Clamp(int value, int min, int max) => Mathf.Clamp(value, min, max);
        public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max) => new Vector2(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y));
        public static Vector2Int Clamp(Vector2Int value, Vector2Int min, Vector2Int max) => new Vector2Int(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y));
        public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max) => new Vector3(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y), Mathf.Clamp(value.z, min.z, max.z));
        public static Vector3Int Clamp(Vector3Int value, Vector3Int min, Vector3Int max) => new Vector3Int(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y), Mathf.Clamp(value.z, min.z, max.z));

        public static Vector3 CenterOfVectors(List<Vector3> vectors)
        {
            Vector3 vector = Vector3.zero;
            if (vectors == null || vectors.Count == 0)
                return vector;
            foreach (Vector3 b in vectors)
                vector += b;
            return vector / (float)vectors.Count;
        }

        public static Vector3 NearestVector(Vector3 a, List<Vector3> vectors)
        {
            float[] distances = new float[vectors.Count];

            int num = 0;
            foreach (var v in vectors)
            {
                distances[num] = Vector3.Distance(a, v);

                num++;
            }

            float x = float.PositiveInfinity;
            num = 0;
            for (int i = 0; i < distances.Length; i++)
            {
                if (distances[i] < x)
                {
                    x = distances[i];
                    num = i;
                }
            }

            return vectors[num];
        }

        public static Vector3 FurthestVector(Vector3 a, List<Vector3> vectors)
        {
            float[] distances = new float[vectors.Count];

            int num = 0;
            foreach (var v in vectors)
            {
                distances[num] = Vector3.Distance(a, v);

                num++;
            }

            float x = 0f;
            num = 0;
            for (int i = 0; i < distances.Length; i++)
            {
                if (distances[i] > x)
                {
                    x = distances[i];
                    num = i;
                }
            }

            return vectors[num];
        }

        public static float roundToNearest(float value, float multipleOf) => (float)Math.Round((decimal)value / (decimal)multipleOf, MidpointRounding.AwayFromZero) * multipleOf;

        public static Rect RectTransformToScreenSpace(RectTransform transform)
        {
            Vector2 vector = Vector2.Scale(transform.rect.size, transform.lossyScale);
            Rect result = new Rect(transform.position.x, (float)Screen.height - transform.position.y, vector.x, vector.y);
            result.x -= transform.pivot.x * vector.x;
            result.y -= (1f - transform.pivot.y) * vector.y;
            return result;
        }

        public static Rect RectTransformToScreenSpace2(RectTransform transform)
        {
            Vector2 vector = Vector2.Scale(transform.rect.size, transform.lossyScale);
            float x = transform.position.x + transform.anchoredPosition.x;
            float y = (float)Screen.height - transform.position.y - transform.anchoredPosition.y;
            return new Rect(x, y, vector.x, vector.y);
        }

        public static float InterpolateOverCurve(AnimationCurve curve, float from, float to, float t) => from + curve.Evaluate(t) * (to - from);

        public static Vector3 SphericalToCartesian(int radius, int polar)
        {
            return new Vector3
            {
                x = (float)radius * Mathf.Cos(0.017453292f * (float)polar),
                y = (float)radius * Mathf.Sin(0.017453292f * (float)polar)
            };
        }

        public static float SuperLerp(float OldMin, float OldMax, float NewMin, float NewMax, float OldValue)
        {
            float num = OldMax - OldMin;
            float num2 = NewMax - NewMin;
            return (OldValue - OldMin) * num2 / num + NewMin;
        }

        public static Rect ClampToScreen(Rect r)
        {
            r.x = Mathf.Clamp(r.x, 0f, (float)Screen.width - r.width);
            r.y = Mathf.Clamp(r.y, 0f, (float)Screen.height - r.height);
            return r;
        }

        public static float RoundToNearestDecimal(float _value, int _places = 3)
        {
            if (_places <= 0)
            {
                return Mathf.Round(_value);
            }
            int num = 10;
            for (int i = 1; i < _places; i++)
            {
                num *= 10;
            }
            return Mathf.Round(_value * (float)num) / (float)num;
        }

        public static float Distance(float x, float y) => x > y ? -(-x + y) : (-x + y);

        public static float InverseLerp(float x, float y, float t) => (t - x) / (y - x);

        public static float Percentage(float t, float length) => t / length * 100f;

        static float VectorAngle90(Vector2 vector2) => vector2 == Vector2.zero ? 0f : ((vector2.normalized.x - vector2.normalized.y) + 1f) * 45f;

        public static Vector3 Move(Vector3 a, Vector2 b) => new Vector3(a.x + b.x, a.y + b.y, a.z);
        public static Vector3 Scale(Vector3 a, Vector2 b) => new Vector3(a.x * b.x, a.y * b.y, a.z);
        public static Vector3 Rotate(Vector3 a, float b) => Quaternion.Euler(0, 0, b) * a;

        public static float VectorAngle(float x, float y) => VectorAngle(new Vector3(x, y));

        public static float VectorAngle(Vector3 from, Vector3 to) => VectorAngle(new Vector3((-from.x + to.x), (-from.y + to.y)));

        public static float VectorAngle(Vector3 targetVector)
        {
            float x = targetVector.x;
            float y = targetVector.y;

            bool downRight = x >= 0f && y <= 0f;
            bool downLeft = x <= 0f && y <= 0f;
            bool upLeft = x <= 0f && y >= 0f;
            bool upRight = x >= 0f && y >= 0f;

            var vector = upRight ? targetVector : downRight ? Rotate(targetVector, 90f) : downLeft ? Rotate(targetVector, 180f) : upLeft ? Rotate(targetVector, 270f) : targetVector;

            return targetVector == Vector3.zero ? 0f : ((vector.normalized.x - vector.normalized.y) + 1f) * 45f + (upRight ? 0f : downRight ? 90f : downLeft ? 180f : upLeft ? 270f : 0f);
        }
    }
}
