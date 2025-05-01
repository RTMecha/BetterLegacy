using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using ILMath;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Core
{
    /// <summary>
    /// Math helper class.
    /// </summary>
    public static class RTMath
    {
        #region Parse

        /// <summary>
        /// Tries to parse a math expression.
        /// </summary>
        /// <param name="input">Input expression.</param>
        /// <param name="defaultValue">Default value to output if the evaluation failed.</param>
        /// <param name="result">Output value.</param>
        /// <returns>Returns true if the math evaluation was successful, otherwise returns false.</returns>
        public static bool TryParse(string input, float defaultValue, out float result)
        {
            try
            {
                result = Parse(input);
                return true;
            }
            catch
            {
                result = defaultValue;
                return false;
            }
        }

        /// <summary>
        /// Tries to parse a math expression.
        /// </summary>
        /// <param name="input">Input expression.</param>
        /// <param name="defaultValue">Default value to output if the evaluation failed.</param>
        /// <param name="variables">Custom variables to register.</param>
        /// <param name="result">Output value.</param>
        /// <returns>Returns true if the math evaluation was successful, otherwise returns false.</returns>
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

        /// <summary>
        /// Tries to parse a math expression.
        /// </summary>
        /// <param name="input">Input expression.</param>
        /// <param name="defaultValue">Default value to output if the evaluation failed.</param>
        /// <param name="variables">Custom variables to register.</param>
        /// <param name="functions">Custom functions to register.</param>
        /// <param name="result">Output value.</param>
        /// <returns>Returns true if the math evaluation was successful, otherwise returns false.</returns>
        public static bool TryParse(string input, float defaultValue, Dictionary<string, float> variables, Dictionary<string, MathFunction> functions, out float result)
        {
            try
            {
                result = Parse(input, variables, functions);
                return true;
            }
            catch
            {
                result = defaultValue;
                return false;
            }
        }

        // RTMath.Parse("pitch + clamp(pitch, 0, 1) * pitch");

        /// <summary>
        /// Parses a math expression.
        /// </summary>
        /// <param name="input">Input expression.</param>
        /// <param name="variables">Custom variables to register.</param>
        /// <param name="functions">Custom functions to register.</param>
        /// <returns>Returns evaluated expression.</returns>
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
                    if (RTLevel.Current)
                        context.RegisterVariable("smoothedTime", RTLevel.Current.CurrentTime);
                    context.RegisterVariable("playerHealthTotal", InputDataManager.inst.players.IsEmpty() ? 0 : PlayerManager.Players.Sum(x => x.Health));
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

                    if (RTLevel.Current)
                        context.RegisterFunction("sampleAudio", parameters => RTLevel.Current.GetSample((int)parameters[0], (float)parameters[1]));
                    if (RTLevel.Current && RTLevel.Current.eventEngine)
                        context.RegisterFunction("copyEvent", parameters => RTLevel.Current.eventEngine.Interpolate((int)parameters[0], (int)parameters[1], (float)parameters[2]));
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
                context.RegisterVariable("currentEpoch", SteamworksFacepunch.Epoch.Current);

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

        #endregion

        #region Lerp

        /// <summary>
        /// Lerps between two values.
        /// </summary>
        /// <param name="start">Start value.</param>
        /// <param name="end">End value.</param>
        /// <param name="t">Time scale.</param>
        /// <returns>Returns interpolated value.</returns>
        public static double Lerp(double start, double end, double t) => start + (end - start) * t;

        /// <summary>
        /// Lerps between two values.
        /// </summary>
        /// <param name="start">Start value.</param>
        /// <param name="end">End value.</param>
        /// <param name="t">Time scale.</param>
        /// <returns>Returns interpolated value.</returns>
        public static float Lerp(float start, float end, float t) => start + (end - start) * t;

        /// <summary>
        /// Lerps between two values.
        /// </summary>
        /// <param name="start">Start value.</param>
        /// <param name="end">End value.</param>
        /// <param name="t">Time scale.</param>
        /// <returns>Returns interpolated value.</returns>
        public static Vector2 Lerp(Vector2 start, Vector2 end, float t) => start + (end - start) * t;

        /// <summary>
        /// Lerps between two values.
        /// </summary>
        /// <param name="start">Start value.</param>
        /// <param name="end">End value.</param>
        /// <param name="t">Time scale.</param>
        /// <returns>Returns interpolated value.</returns>
        public static Vector3 Lerp(Vector3 start, Vector3 end, float t) => start + (end - start) * t;

        /// <summary>
        /// Lerps between two values.
        /// </summary>
        /// <param name="start">Start value.</param>
        /// <param name="end">End value.</param>
        /// <param name="t">Time scale.</param>
        /// <returns>Returns interpolated value.</returns>
        public static Color Lerp(Color start, Color end, float t) => start + (end - start) * t;

        /// <summary>
        /// Lerps between two ranges of values.
        /// </summary>
        /// <param name="oldMin">Old min value.</param>
        /// <param name="oldMax">Old max value.</param>
        /// <param name="newMin">New min value.</param>
        /// <param name="newMax">New max value.</param>
        /// <param name="t">Time scale.</param>
        /// <returns>Returns interpolated value.</returns>
        public static float SuperLerp(float oldMin, float oldMax, float newMin, float newMax, float t) => (t - oldMin) * (newMax - newMin) / (oldMax - oldMin) + newMin;

        /// <summary>
        /// Lerps between two values from the starting value.
        /// </summary>
        /// <param name="start">Start value.</param>
        /// <param name="end">End value.</param>
        /// <param name="t">Time scale.</param>
        /// <returns>Returns interpolated value.</returns>
        public static double InverseLerp(double start, double end, double t) => (t - start) / (end - start);

        /// <summary>
        /// Lerps between two values from the starting value.
        /// </summary>
        /// <param name="start">Start value.</param>
        /// <param name="end">End value.</param>
        /// <param name="t">Time scale.</param>
        /// <returns>Returns interpolated value.</returns>
        public static float InverseLerp(float start, float end, float t) => (t - start) / (end - start);

        #endregion

        #region Clamp

        /// <summary>
        /// Clamps a value.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum the value can be.</param>
        /// <param name="max">Maximum the value can be.</param>
        /// <returns>Returns a clamped value.</returns>
        public static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

        /// <summary>
        /// Clamps a value.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum the value can be.</param>
        /// <param name="max">Maximum the value can be.</param>
        /// <returns>Returns a clamped value.</returns>
        public static float Clamp(float value, float min, float max) => Mathf.Clamp(value, min, max);

        /// <summary>
        /// Clamps a value.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum the value can be.</param>
        /// <param name="max">Maximum the value can be.</param>
        /// <returns>Returns a clamped value.</returns>
        public static int Clamp(int value, int min, int max) => Mathf.Clamp(value, min, max);

        /// <summary>
        /// Clamps a value.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum the value can be.</param>
        /// <param name="max">Maximum the value can be.</param>
        /// <returns>Returns a clamped value.</returns>
        public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max) => new Vector2(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y));

        /// <summary>
        /// Clamps a value.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum the value can be.</param>
        /// <param name="max">Maximum the value can be.</param>
        /// <returns>Returns a clamped value.</returns>
        public static Vector2Int Clamp(Vector2Int value, Vector2Int min, Vector2Int max) => new Vector2Int(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y));

        /// <summary>
        /// Clamps a value.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum the value can be.</param>
        /// <param name="max">Maximum the value can be.</param>
        /// <returns>Returns a clamped value.</returns>
        public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max) => new Vector3(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y), Mathf.Clamp(value.z, min.z, max.z));

        /// <summary>
        /// Clamps a value.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum the value can be.</param>
        /// <param name="max">Maximum the value can be.</param>
        /// <returns>Returns a clamped value.</returns>
        public static Vector3Int Clamp(Vector3Int value, Vector3Int min, Vector3Int max) => new Vector3Int(Mathf.Clamp(value.x, min.x, max.x), Mathf.Clamp(value.y, min.y, max.y), Mathf.Clamp(value.z, min.z, max.z));

        /// <summary>
        /// Clamps a value if neither min nor max are zero.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum the value can be.</param>
        /// <param name="max">Maximum the value can be.</param>
        /// <returns>Reutrns a clamped value if min nor max are zero, otherwise returns the value.</returns>
        public static double ClampZero(double value, double min, double max) => min != 0.0 || max != 0.0 ? Clamp(value, min, max) : value;

        /// <summary>
        /// Clamps a value if neither min nor max are zero.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum the value can be.</param>
        /// <param name="max">Maximum the value can be.</param>
        /// <returns>Reutrns a clamped value if min nor max are zero, otherwise returns the value.</returns>
        public static float ClampZero(float value, float min, float max) => min != 0f || max != 0f ? Clamp(value, min, max) : value;

        /// <summary>
        /// Clamps a value if neither min nor max are zero.
        /// </summary>
        /// <param name="value">Value to clamp.</param>
        /// <param name="min">Minimum the value can be.</param>
        /// <param name="max">Maximum the value can be.</param>
        /// <returns>Reutrns a clamped value if min nor max are zero, otherwise returns the value.</returns>
        public static int ClampZero(int value, int min, int max) => min != 0 || max != 0 ? Clamp(value, min, max) : value;

        #endregion

        #region Vectors

        /// <summary>
        /// Gets the center of a collection of vectors.
        /// </summary>
        /// <param name="vectors">Collection of vectors to find the center of.</param>
        /// <returns>Returns the collections' center.</returns>
        public static Vector3 CenterOfVectors(IEnumerable<Vector3> vectors)
        {
            var vector = Vector3.zero;
            if (vectors == null || vectors.Count() == 0)
                return vector;
            foreach (var b in vectors)
                vector += b;
            return vector / vectors.Count();
        }

        /// <summary>
        /// Gets the nearest vector to <paramref name="vector"/>.
        /// </summary>
        /// <param name="vector">Vector reference.</param>
        /// <param name="vectors">Collection of vectors.</param>
        /// <returns>Returns the vector nearest to the provided vector.</returns>
        public static Vector3 NearestVector(Vector3 vector, IEnumerable<Vector3> vectors)
        {
            if (vectors == null || vectors.Count() == 0)
                return vector;

            return OrderByClosest(vector, vectors).ElementAt(0);
        }

        /// <summary>
        /// Gets the furthest vector to <paramref name="vector"/>.
        /// </summary>
        /// <param name="vector">Vector reference.</param>
        /// <param name="vectors">Collection of vectors.</param>
        /// <returns>Returns the vector furthest to the provided vector.</returns>
        public static Vector3 FurthestVector(Vector3 vector, IEnumerable<Vector3> vectors)
        {
            if (vectors == null || vectors.Count() == 0)
                return vector;

            return OrderByFurthest(vector, vectors).ElementAt(0);
        }

        /// <summary>
        /// Orders a collection of vectors closest to a vector.
        /// </summary>
        /// <param name="vector">Vector reference.</param>
        /// <param name="vectors">Collection of vectors.</param>
        /// <returns>Returns an ordered collection of vectors by closeness.</returns>
        public static IEnumerable<Vector3> OrderByClosest(Vector3 vector, IEnumerable<Vector3> vectors) => vectors.OrderBy(x => Vector3.Distance(vector, x));

        /// <summary>
        /// Orders a collection of vectors furthest to a vector.
        /// </summary>
        /// <param name="vector">Vector reference.</param>
        /// <param name="vectors">Collection of vectors.</param>
        /// <returns>Returns an ordered collection of vectors by furthness.</returns>
        public static IEnumerable<Vector3> OrderByFurthest(Vector3 vector, IEnumerable<Vector3> vectors) => vectors.OrderByDescending(x => Vector3.Distance(vector, x));

        #endregion

        #region Rounding

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

        /// <summary>
        /// Rounds a double to int.
        /// </summary>
        /// <param name="num">Value to round.</param>
        /// <returns>Returns the value rounded to an integer.</returns>
        public static int Round(double num) => (int)Math.Round(num);

        #endregion

        #region Translate

        /// <summary>
        /// Gets the distance between <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>Returns the distance between the two values.</returns>
        public static double Distance(double a, double b) => a > b ? -(-a + b) : (-a + b);

        /// <summary>
        /// Gets the distance between <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>Returns the distance between the two values.</returns>
        public static float Distance(float a, float b) => a > b ? -(-a + b) : (-a + b);

        /// <summary>
        /// Gets the distance between <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>Returns the distance between the two values.</returns>
        public static float Distance(int a, int b) => a > b ? -(-a + b) : (-a + b);
        
        /// <summary>
        /// Gets the distance between <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>Returns the distance between the two values.</returns>
        public static float Distance(Vector2 a, Vector2 b) => Vector2.Distance(a, b);
        
        /// <summary>
        /// Gets the distance between <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>Returns the distance between the two values.</returns>
        public static float Distance(Vector2Int a, Vector2Int b) => Vector2Int.Distance(a, b);
        
        /// <summary>
        /// Gets the distance between <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>Returns the distance between the two values.</returns>
        public static float Distance(Vector3 a, Vector3 b) => Vector3.Distance(a, b);
        
        /// <summary>
        /// Gets the distance between <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>Returns the distance between the two values.</returns>
        public static float Distance(Vector3Int a, Vector3Int b) => Vector3Int.Distance(a, b);

        /// <summary>
        /// Moves a provided position value.
        /// </summary>
        /// <param name="pos">Original position.</param>
        /// <param name="move">Move amount to add.</param>
        /// <returns>Returns a moved vector.</returns>
        public static Vector3 Move(Vector3 pos, Vector2 move) => new Vector3(pos.x + move.x, pos.y + move.y, pos.z);

        /// <summary>
        /// Moves a provided position value.
        /// </summary>
        /// <param name="pos">Original position.</param>
        /// <param name="move">Move amount to add.</param>
        /// <returns>Returns a moved vector.</returns>
        public static Vector3 Move(Vector3 pos, Vector3 move) => new Vector3(pos.x + move.x, pos.y + move.y, pos.z + move.z);

        /// <summary>
        /// Scales a provided scale value.
        /// </summary>
        /// <param name="sca">Original scale.</param>
        /// <param name="scale">Scale to scale to.</param>
        /// <returns>Returns a scaled vector.</returns>
        public static Vector3 Scale(Vector3 sca, Vector2 scale) => new Vector3(sca.x * scale.x, sca.y * scale.y, sca.z);

        /// <summary>
        /// Scales a provided scale value.
        /// </summary>
        /// <param name="sca">Original scale.</param>
        /// <param name="scale">Scale to scale to.</param>
        /// <returns>Returns a scaled vector.</returns>
        public static Vector3 Scale(Vector3 sca, Vector3 scale) => new Vector3(sca.x * scale.x, sca.y * scale.y, sca.z * scale.z);

        /// <summary>
        /// Rotates a provided rotation.
        /// </summary>
        /// <param name="rot">Original rotation.</param>
        /// <param name="rotate">Rotation to rotate to.</param>
        /// <returns>Returns a rotated vector.</returns>
        public static Vector3 Rotate(Vector3 rot, float rotate) => Quaternion.Euler(0, 0, rotate) * rot;

        /// <summary>
        /// Calculates the angle from the one vector to another vector.
        /// </summary>
        /// <param name="from">Vector that will look at the target..</param>
        /// <param name="to">Target vector.</param>
        /// <returns>Returns a angle that is looking at the target.</returns>
        public static float VectorAngle(Vector3 from, Vector3 to) => VectorAngle(new Vector3((-from.x + to.x), (-from.y + to.y)));

        /// <summary>
        /// Calculates the angle from the center vector to a target vector.
        /// </summary>
        /// <param name="targetVector">Target to angle.</param>
        /// <returns>Returns a angle that is looking at the target.</returns>
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

        #endregion

        #region Operations

        /// <summary>
        /// Performs any specified math operations, such as addition, subtraction, etc.
        /// </summary>
        /// <param name="num">Reference number to apply the operation to.</param>
        /// <param name="value">Value to apply.</param>
        /// <param name="operation">Math operator to use.</param>
        public static void Operation(ref double num, double value, MathOperation operation)
        {
            switch (operation)
            {
                case MathOperation.Addition: {
                        num += value;
                        break;
                    }
                case MathOperation.Subtract: {
                        num -= value;
                        break;
                    }
                case MathOperation.Multiply: {
                        num *= value;
                        break;
                    }
                case MathOperation.Divide: {
                        num /= value;
                        break;
                    }
                case MathOperation.Modulo: {
                        num %= value;
                        break;
                    }
                case MathOperation.Set: {
                        num = value;
                        break;
                    }
            }
        }

        /// <summary>
        /// Performs any specified math operations, such as addition, subtraction, etc.
        /// </summary>
        /// <param name="num">Reference number to apply the operation to.</param>
        /// <param name="value">Value to apply.</param>
        /// <param name="operation">Math operator to use.</param>
        public static void Operation(ref float num, float value, MathOperation operation)
        {
            switch (operation)
            {
                case MathOperation.Addition: {
                        num += value;
                        break;
                    }
                case MathOperation.Subtract: {
                        num -= value;
                        break;
                    }
                case MathOperation.Multiply: {
                        num *= value;
                        break;
                    }
                case MathOperation.Divide: {
                        num /= value;
                        break;
                    }
                case MathOperation.Modulo: {
                        num %= value;
                        break;
                    }
                case MathOperation.Set: {
                        num = value;
                        break;
                    }
            }
        }

        /// <summary>
        /// Performs any specified math operations, such as addition, subtraction, etc.
        /// </summary>
        /// <param name="num">Reference number to apply the operation to.</param>
        /// <param name="value">Value to apply.</param>
        /// <param name="operation">Math operator to use.</param>
        public static void Operation(ref int num, int value, MathOperation operation)
        {
            switch (operation)
            {
                case MathOperation.Addition: {
                        num += value;
                        break;
                    }
                case MathOperation.Subtract: {
                        num -= value;
                        break;
                    }
                case MathOperation.Multiply: {
                        num *= value;
                        break;
                    }
                case MathOperation.Divide: {
                        num /= value;
                        break;
                    }
                case MathOperation.Modulo: {
                        num %= value;
                        break;
                    }
                case MathOperation.Set: {
                        num = value;
                        break;
                    }
            }
        }

        /// <summary>
        /// Performs any specified math operations, such as addition, subtraction, etc.
        /// </summary>
        /// <param name="num">Reference number to apply the operation to.</param>
        /// <param name="value">Value to apply.</param>
        /// <param name="operation">Math operator to use.</param>
        public static void Operation(ref Vector2 num, Vector2 value, MathOperation operation)
        {
            switch (operation)
            {
                case MathOperation.Addition: {
                        num += value;
                        break;
                    }
                case MathOperation.Subtract: {
                        num -= value;
                        break;
                    }
                case MathOperation.Multiply: {
                        num *= value;
                        break;
                    }
                case MathOperation.Divide: {
                        num /= value;
                        break;
                    }
                case MathOperation.Modulo: {
                        num.x %= value.x;
                        num.y %= value.y;
                        break;
                    }
                case MathOperation.Set: {
                        num = value;
                        break;
                    }
            }
        }

        /// <summary>
        /// Performs any specified math operations, such as addition, subtraction, etc.
        /// </summary>
        /// <param name="num">Reference number to apply the operation to.</param>
        /// <param name="value">Value to apply.</param>
        /// <param name="operation">Math operator to use.</param>
        public static void Operation(ref Vector3 num, Vector3 value, MathOperation operation)
        {
            switch (operation)
            {
                case MathOperation.Addition: {
                        num += value;
                        break;
                    }
                case MathOperation.Subtract: {
                        num -= value;
                        break;
                    }
                case MathOperation.Multiply: {
                        num.x *= value.x;
                        num.y *= value.y;
                        break;
                    }
                case MathOperation.Divide: {
                        num.x /= value.x;
                        num.y /= value.y;
                        break;
                    }
                case MathOperation.Modulo: {
                        num.x %= value.x;
                        num.y %= value.y;
                        break;
                    }
                case MathOperation.Set: {
                        num = value;
                        break;
                    }
            }
        }

        /// <summary>
        /// Performs any specified math operations, such as addition, subtraction, etc.
        /// </summary>
        /// <param name="num">Reference number to apply the operation to.</param>
        /// <param name="value">Value to apply.</param>
        /// <param name="operation">Math operator to use.</param>
        public static void Operation(ref Color num, Color value, MathOperation operation)
        {
            switch (operation)
            {
                case MathOperation.Addition: {
                        num += value;
                        break;
                    }
                case MathOperation.Subtract: {
                        num -= value;
                        break;
                    }
                case MathOperation.Multiply: {
                        num *= value;
                        break;
                    }
                case MathOperation.Divide: {
                        if (value.r != 0f)
                            num.r /= value.r;
                        if (value.g != 0f)
                            num.g /= value.g;
                        if (value.b != 0f)
                            num.b /= value.b;
                        if (value.a != 0f)
                            num.a /= value.a;
                        break;
                    }
                case MathOperation.Modulo: {
                        num.r %= value.r;
                        num.g %= value.g;
                        num.b %= value.b;
                        num.a %= value.a;
                        break;
                    }
                case MathOperation.Set: {
                        num = value;
                        break;
                    }
            }
        }

        #endregion

        #region Misc

        /// <summary>
        /// Checks if the number is an incompatible number.
        /// </summary>
        /// <param name="f">Number to check.</param>
        /// <returns>Returns true if the number is incompatible, otherwise returns false.</returns>
        public static bool IsNaNInfinity(float f) => float.IsNaN(f) || float.IsInfinity(f);

        /// <summary>
        /// Gets the screen space rect of a <see cref="RectTransform"/>.
        /// </summary>
        /// <param name="transform">RectTransform to get the screen space area of.</param>
        /// <returns>Returns a <see cref="Rect"/> based on the <paramref name="transform"/>.</returns>
        public static Rect RectTransformToScreenSpace(RectTransform transform)
        {
            Vector2 vector = Vector2.Scale(transform.rect.size, transform.lossyScale);
            Rect result = new Rect(transform.position.x, (float)Screen.height - transform.position.y, vector.x, vector.y);
            result.x -= transform.pivot.x * vector.x;
            result.y -= (1f - transform.pivot.y) * vector.y;
            return result;
        }

        /// <summary>
        /// Converts two values into a percentage.
        /// </summary>
        /// <param name="t">The percentage value.</param>
        /// <param name="length">Total percent.</param>
        /// <returns>Returns a calculated percentage.</returns>
        public static float Percentage(float t, float length) => t / length * 100f;

        /// <summary>
        /// Recursively multiplies.
        /// </summary>
        /// <param name="t">Amount to multiply each count.</param>
        /// <param name="count">Amount of times to multiply.</param>
        /// <returns>Returns a multiplied recursive calculation.</returns>
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

        #endregion
    }
}
