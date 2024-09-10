using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Helpers;
using System.Text.RegularExpressions;

namespace BetterLegacy.Core
{
    public static class RTMath
    {
        // from https://stackoverflow.com/questions/355062/is-there-a-string-math-evaluator-in-net
        public static double Evaluate(string str) => Convert.ToDouble(new DataTable().Compute(str, null));

        public static string Replace(string input)
        {
            try
            {
                if (CoreHelper.RegexMatch(input, new Regex(@"player([0-9]+)PosX"), out Match matchX))
                {
                    var baseString = matchX.Groups[0].ToString();
                    var index = Mathf.Clamp(Parser.TryParse(matchX.Groups[1].ToString(), 0), 1, int.MaxValue) - 1;

                    if (PlayerManager.Players.Count <= index || !PlayerManager.Players[index].Player || !PlayerManager.Players[index].Player.rb)
                        input = input.Replace(baseString, "0");
                    else
                        input = input.Replace(baseString, PlayerManager.Players[0].Player.rb.position.x.ToString());
                }
                if (CoreHelper.RegexMatch(input, new Regex(@"player([0-9]+)PosY"), out Match matchY))
                {
                    var baseString = matchY.Groups[0].ToString();
                    var index = Mathf.Clamp(Parser.TryParse(matchY.Groups[1].ToString(), 0), 1, int.MaxValue) - 1;

                    if (PlayerManager.Players.Count <= index || !PlayerManager.Players[index].Player || !PlayerManager.Players[index].Player.rb)
                        input = input.Replace(baseString, "0");
                    else
                        input = input.Replace(baseString, PlayerManager.Players[0].Player.rb.position.y.ToString());
                }

                return input
                    .Replace("actionMoveX", InputDataManager.inst.menuActions.Move.X.ToString())
                    .Replace("actionMoveY", InputDataManager.inst.menuActions.Move.Y.ToString())
                    .Replace("time", Time.time.ToString())
                    .Replace("deltaTime", Time.deltaTime.ToString())
                    .Replace("audioTime", AudioManager.inst.CurrentAudioSource.time.ToString());
            }
            catch
            {
                return input;
            }
        }

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

        public static float ClampZero(float value, float min, float max)
            => min != 0f || max != 0f ? Clamp(value, min, max) : value;
        
        public static float ClampZero(int value, int min, int max)
            => min != 0 || max != 0 ? Clamp(value, min, max) : value;

        public static Vector3 CenterOfVectors(IEnumerable<Vector3> vectors)
        {
            var vector = Vector3.zero;
            if (vectors == null || vectors.Count() == 0)
                return vector;
            foreach (var b in vectors)
                vector += b;
            return vector / vectors.Count();
        }

        public static Vector3 NearestVector(Vector3 a, IEnumerable<Vector3> vectors)
        {
            if (vectors == null || vectors.Count() == 0)
                return a;

            return OrderByClosest(a, vectors).ElementAt(0);
        }

        public static Vector3 FurthestVector(Vector3 a, IEnumerable<Vector3> vectors)
        {
            if (vectors == null || vectors.Count() == 0)
                return a;

            return OrderByFurthest(a, vectors).ElementAt(0);
        }

        public static IEnumerable<Vector3> OrderByClosest(Vector3 a, IEnumerable<Vector3> vectors) => vectors.OrderBy(x => Vector3.Distance(a, x));
        public static IEnumerable<Vector3> OrderByFurthest(Vector3 a, IEnumerable<Vector3> vectors) => vectors.OrderByDescending(x => Vector3.Distance(a, x));

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

        public static float SuperLerp(float oldMin, float oldMax, float newMin, float newMax, float oldValue)
        {
            float oldResult = oldMax - oldMin;
            float newResult = newMax - newMin;
            return (oldValue - oldMin) * newResult / oldResult + newMin;
        }

        /// <summary>
        /// Snaps the value to a multiple of a specific number. For example, if 'value' is 4 and multipleOf is 5, it will round to 5.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="multipleOf"></param>
        /// <returns>Returns a rounded number.</returns>
        public static float RoundToNearestNumber(float value, float multipleOf) => (float)Math.Round((decimal)value / (decimal)multipleOf, MidpointRounding.AwayFromZero) * multipleOf;

        /// <summary>
        /// Shortens the decimal count.
        /// </summary>
        /// <param name="value">The number to shorten.</param>
        /// <param name="places">The decimal count to shorten to.</param>
        /// <returns>Returns a shortened number.</returns>
        public static float RoundToNearestDecimal(float value, int places = 3)
        {
            if (places <= 0)
                return Mathf.Round(value);

            int num = 10;
            for (int i = 1; i < places; i++)
                num *= 10;

            return Mathf.Round(value * num) / num;
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
