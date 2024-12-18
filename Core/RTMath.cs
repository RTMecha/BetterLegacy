using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Optimization;
using ILMath;

namespace BetterLegacy.Core
{
    public static class RTMath
    {
        public static bool TryParse(string input, float defaultValue, Dictionary<string, float> variables, out float result)
        {
            try
            {
                result = Parse(input, variables);
                return true;
            }
            catch
            {
                result = defaultValue;
                return false;
            }
        }

        // RTMath.Parse("pitch + clamp(pitch, 0, 1) * pitch");

        public static float Parse(string input, Dictionary<string, float> variables = null, Dictionary<string, MathFunction> functions = null)
        {
            try
            {
                input = input.Replace(";", ""); // replaces ; due to really old math evaluator
                input = input.Replace("True", "1").Replace("true", "1").Replace("False", "0").Replace("false", "0"); // replaces true / false syntax with their number equivalents.
                var context = EvaluationContext.CreateDefault();

                if (variables != null)
                    foreach (var variable in variables)
                        context.RegisterVariable(variable.Key, variable.Value);

                // In-game only variables and functions
                if (CoreHelper.InGame)
                {
                    context.RegisterVariable("deathCount", GameManager.inst.deaths.Count);
                    context.RegisterVariable("hitCount", GameManager.inst.hits.Count);
                    context.RegisterVariable("boostCount", LevelManager.BoostCount);
                    context.RegisterVariable("smoothedTime", RTEventManager.inst.currentTime);
                    context.RegisterVariable("playerHealthTotal", PlayerManager.Players.Sum(x => x.Health));
                    context.RegisterVariable("camPosX", EventManager.inst.cam.transform.position.x);
                    context.RegisterVariable("camPosY", EventManager.inst.cam.transform.position.y);
                    context.RegisterVariable("camZoom", EventManager.inst.cam.orthographicSize);
                    context.RegisterVariable("camRot", EventManager.inst.cam.transform.localEulerAngles.z);
                    context.RegisterVariable("currentSeed", RandomHelper.CurrentSeed.GetHashCode());

                    var players = PlayerManager.Players;
                    for (int i = 0; i < players.Count; i++)
                    {
                        var player = players[i];
                        var isNull = !player.Player || !player.Player.rb;
                        float posX = isNull ? 0f : player.Player.rb.position.x;
                        float posY = isNull ? 0f : player.Player.rb.position.y;
                        float rot = isNull ? 0f : player.Player.rb.rotation;
                        context.RegisterVariable($"player{i}PosX", posX);
                        context.RegisterVariable($"player{i}PosY", posY);
                        context.RegisterVariable($"player{i}Rot", rot);
                        context.RegisterVariable($"player{i}Health", player.Health);
                    }

                    context.RegisterFunction("sampleAudio", parameters => Updater.GetSample((int)parameters[0], (float)parameters[1]));
                    context.RegisterFunction("copyEvent", parameters => RTEventManager.inst.Interpolate((int)parameters[0], (int)parameters[1], (float)parameters[2]));
                }

                context.RegisterVariable("actionMoveX", InputDataManager.inst.menuActions.Move.X);
                context.RegisterVariable("actionMoveY", InputDataManager.inst.menuActions.Move.Y);
                context.RegisterVariable("time", Time.time);
                context.RegisterVariable("deltaTime", Time.deltaTime);
                context.RegisterVariable("audioTime", AudioManager.inst.CurrentAudioSource.time);
                context.RegisterVariable("volume", AudioManager.inst.musicVol);
                context.RegisterVariable("pitch", AudioManager.inst.pitch);
                context.RegisterVariable("forwardPitch", CoreHelper.ForwardPitch);
                context.RegisterVariable("mousePosX", Input.mousePosition.x);
                context.RegisterVariable("mousePosY", Input.mousePosition.y);
                context.RegisterVariable("screenHeight", Screen.height);
                context.RegisterVariable("screenWidth", Screen.width);

                context.RegisterFunction("clampZero", parameters => ClampZero(parameters[0], parameters[1], parameters[2]));
                context.RegisterFunction("lerpAngle", parameters => Mathf.LerpAngle((float)parameters[0], (float)parameters[1], (float)parameters[2]));
                context.RegisterFunction("moveTowards", parameters => Mathf.MoveTowards((float)parameters[0], (float)parameters[1], (float)parameters[2]));
                context.RegisterFunction("moveTowardsAngle", parameters => Mathf.MoveTowardsAngle((float)parameters[0], (float)parameters[1], (float)parameters[2]));
                context.RegisterFunction("smoothStep", parameters => Mathf.SmoothStep((float)parameters[0], (float)parameters[1], (float)parameters[2]));
                context.RegisterFunction("gamma", parameters => Mathf.Gamma((float)parameters[0], (float)parameters[1], (float)parameters[2]));
                context.RegisterFunction("repeat", parameters => Mathf.Repeat((float)parameters[0], (float)parameters[1]));
                context.RegisterFunction("pingPong", parameters => Mathf.PingPong((float)parameters[0], (float)parameters[1]));
                context.RegisterFunction("deltaAngle", parameters => Mathf.DeltaAngle((float)parameters[0], (float)parameters[1]));
                context.RegisterFunction("random", parameters => parameters.Length switch
                {
                    0 => new System.Random().NextDouble(),
                    1 => new System.Random((int)parameters[0]).NextDouble(),
                    2 => RandomHelper.SingleFromIndex(parameters[0].ToString(), (int)parameters[1]),
                    _ => 0
                });
                context.RegisterFunction("randomSeed", parameters => parameters.Length switch
                {
                    0 => new System.Random(RandomHelper.CurrentSeed.GetHashCode()).NextDouble(),
                    1 => new System.Random((int)parameters[0] ^ RandomHelper.CurrentSeed.GetHashCode()).NextDouble(),
                    _ => 0
                });
                context.RegisterFunction("randomRange", parameters => parameters.Length switch
                {
                    2 => RandomHelper.SingleFromRange(new System.Random().Next().ToString(), (float)parameters[0], (float)parameters[1]),
                    3 => RandomHelper.SingleFromIndexRange(parameters[0].ToString(), new System.Random().Next(), (float)parameters[1], (float)parameters[2]),
                    4 => RandomHelper.SingleFromIndexRange(parameters[0].ToString(), (int)parameters[1], (float)parameters[2], (float)parameters[3]),
                    _ => 0
                });
                context.RegisterFunction("randomSeedRange", parameters => parameters.Length switch
                {
                    2 => RandomHelper.SingleFromRange(RandomHelper.CurrentSeed, (float)parameters[0], (float)parameters[1]),
                    3 => RandomHelper.SingleFromIndexRange(((int)parameters[0] ^ RandomHelper.CurrentSeed.GetHashCode()).ToString(), new System.Random().Next(), (float)parameters[1], (float)parameters[2]),
                    4 => RandomHelper.SingleFromIndexRange(((int)parameters[0] ^ RandomHelper.CurrentSeed.GetHashCode()).ToString(), (int)parameters[1], (float)parameters[2], (float)parameters[3]),
                    _ => 0
                });
                context.RegisterFunction("randomInt", parameters => parameters.Length switch
                {
                    0 => new System.Random().Next(),
                    1 => new System.Random((int)parameters[0]).Next(),
                    2 => RandomHelper.FromIndex(parameters[0].ToString(), (int)parameters[1]),
                    _ => 0
                });
                context.RegisterFunction("randomSeedInt", parameters => parameters.Length switch
                {
                    0 => new System.Random(RandomHelper.CurrentSeed.GetHashCode()).Next(),
                    1 => new System.Random((int)parameters[0] ^ RandomHelper.CurrentSeed.GetHashCode()).Next(),
                    _ => 0
                });
                context.RegisterFunction("randomRangeInt", parameters => parameters.Length switch
                {
                    2 => RandomHelper.FromRange(new System.Random().Next().ToString(), (int)parameters[0], (int)parameters[1]),
                    3 => RandomHelper.FromIndexRange(parameters[0].ToString(), new System.Random().Next(), (int)parameters[1], (int)parameters[2]),
                    4 => RandomHelper.FromIndexRange(parameters[0].ToString(), (int)parameters[1], (int)parameters[2], (int)parameters[3]),
                    _ => 0
                });
                context.RegisterFunction("randomSeedRangeInt", parameters => parameters.Length switch
                {
                    2 => RandomHelper.FromRange(RandomHelper.CurrentSeed, (int)parameters[0], (int)parameters[1]),
                    3 => RandomHelper.FromIndexRange(((int)parameters[0] ^ RandomHelper.CurrentSeed.GetHashCode()).ToString(), new System.Random().Next(), (int)parameters[1], (int)parameters[2]),
                    4 => RandomHelper.FromIndexRange(((int)parameters[0] ^ RandomHelper.CurrentSeed.GetHashCode()).ToString(), (int)parameters[1], (int)parameters[2], (int)parameters[3]),
                    _ => 0
                });
                context.RegisterFunction("roundToNearestNumber", parameters => RoundToNearestNumber((float)parameters[0], (float)parameters[1]));
                context.RegisterFunction("roundToNearestDecimal", parameters => RoundToNearestDecimal((float)parameters[0], (int)parameters[1]));
                context.RegisterFunction("percentage", parameters => Percentage((float)parameters[0], (float)parameters[1]));
                context.RegisterFunction("equals", parameters => parameters[0] == parameters[1] ? parameters[2] : parameters[3]);
                context.RegisterFunction("lesserEquals", parameters => parameters[0] <= parameters[1] ? parameters[2] : parameters[3]);
                context.RegisterFunction("greaterEquals", parameters => parameters[0] >= parameters[1] ? parameters[2] : parameters[3]);
                context.RegisterFunction("lesser", parameters => parameters[0] < parameters[1] ? parameters[2] : parameters[3]);
                context.RegisterFunction("greater", parameters => parameters[0] > parameters[1] ? parameters[2] : parameters[3]);
                context.RegisterFunction("int", parameters => (int)parameters[0]);
                context.RegisterFunction("vectorAngle", parameters => VectorAngle(new Vector3((float)parameters[0], (float)parameters[1], (float)parameters[2]), new Vector3((float)parameters[3], (float)parameters[4], (float)parameters[5])));
                context.RegisterFunction("distance", parameters => parameters.Length switch
                {
                    2 => Distance((float)parameters[0], (float)parameters[1]),
                    4 => Vector2.Distance(new Vector2((float)parameters[0], (float)parameters[1]), new Vector2((float)parameters[2], (float)parameters[3])),
                    6 => Vector3.Distance(new Vector3((float)parameters[0], (float)parameters[1], (float)parameters[2]), new Vector3((float)parameters[3], (float)parameters[4], (float)parameters[5])),
                    _ => 0
                });
                context.RegisterFunction("worldToViewportPointX", parameters =>
                {
                    var position = Camera.main.WorldToViewportPoint(new Vector3((float)parameters[0], parameters.Length > 1 ? (float)parameters[1] : 0f, parameters.Length > 2 ? (float)parameters[2] : 0f));
                    return position.x;
                });
                context.RegisterFunction("worldToViewportPointY", parameters =>
                {
                    var position = Camera.main.WorldToViewportPoint(new Vector3((float)parameters[0], parameters.Length > 1 ? (float)parameters[1] : 0f, parameters.Length > 2 ? (float)parameters[2] : 0f));
                    return position.y;
                });
                context.RegisterFunction("worldToViewportPointZ", parameters =>
                {
                    var position = Camera.main.WorldToViewportPoint(new Vector3((float)parameters[0], parameters.Length > 1 ? (float)parameters[1] : 0f, parameters.Length > 2 ? (float)parameters[2] : 0f));
                    return position.z;
                });
                context.RegisterFunction("mirrorNegative", parameters => parameters[0] < 0 ? -parameters[0] : parameters[0]);
                context.RegisterFunction("mirrorPositive", parameters => parameters[0] > 0 ? -parameters[0] : parameters[0]);

                if (functions != null)
                    foreach (var function in functions)
                        context.RegisterFunction(function.Key, function.Value);

                var evaluator = MathEvaluation.CompileExpression("ResultFunction", input);

                return (float)evaluator.Invoke(context);
            }
            catch
            {
                return 0f;
            }
        }

        public static double Lerp(double x, double y, double t) => x + (y - x) * t;
        public static float Lerp(float x, float y, float t) => x + (y - x) * t;
        public static Vector2 Lerp(Vector2 x, Vector2 y, float t) => x + (y - x) * t;
        public static Vector3 Lerp(Vector3 x, Vector3 y, float t) => x + (y - x) * t;
        public static Color Lerp(Color x, Color y, float t) => x + (y - x) * t;

        public static bool IsNaNInfinity(float f) => float.IsNaN(f) || float.IsInfinity(f);

        public static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

        public static float Clamp(float value, float min, float max) => Mathf.Clamp(value, min, max);
        public static int Clamp(int value, int min, int max) => Mathf.Clamp(value, min, max);
        public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max) => new Vector2(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y));
        public static Vector2Int Clamp(Vector2Int value, Vector2Int min, Vector2Int max) => new Vector2Int(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y));
        public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max) => new Vector3(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y), Mathf.Clamp(value.z, min.z, max.z));
        public static Vector3Int Clamp(Vector3Int value, Vector3Int min, Vector3Int max) => new Vector3Int(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y), Mathf.Clamp(value.z, min.z, max.z));

        public static double ClampZero(double value, double min, double max)
            => min != 0 && max != 0 ? Clamp(value, min, max) : value;

        public static float ClampZero(float value, float min, float max)
            => min != 0f || max != 0f ? Clamp(value, min, max) : value;
        
        public static int ClampZero(int value, int min, int max)
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

        public static double Distance(double a, double b) => a > b ? -(-a + b) : (-a + b);
        public static float Distance(float a, float b) => a > b ? -(-a + b) : (-a + b);

        public static double InverseLerp(double x, double y, double t) => (t - x) / (y - x);
        public static float InverseLerp(float x, float y, float t) => (t - x) / (y - x);

        public static float Percentage(float t, float length) => t / length * 100f;

        public static Vector2 Multiply(Vector2 a, Vector2 b) => new Vector2(a.x * b.x, a.y * b.y);

        public static Vector3 Multiply(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);

        public static float Recursive(float t, int count)
        {
            float result = t;
            int num = count;
            while (num > 1)
            {
                result *= t;

                num--;
            }

            return result;
        }

        public static Vector3 Move(Vector3 a, Vector2 b) => new Vector3(a.x + b.x, a.y + b.y, a.z);
        public static Vector3 Move(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
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
