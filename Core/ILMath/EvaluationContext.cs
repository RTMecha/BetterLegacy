using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Optimization;
using ILMath.Exception;
using System;
using System.Collections.Generic;

namespace ILMath
{

    public delegate double MathFunction(double[] parameters);
    public delegate double CustomFunction(string[] parameters);

    /// <summary>
    /// Default implementation of <see cref="IEvaluationContext"/>.
    /// </summary>
    public class EvaluationContext : IEvaluationContext
    {
        private readonly Dictionary<string, double> variables = new();
        private readonly Dictionary<string, MathFunction> functions = new();

        /// <summary>
        /// Registers default variables and functions to this <see cref="EvaluationContext"/>.
        /// </summary>
        public void RegisterBuiltIns()
        {
            // Register variables
            RegisterVariable("pi", Math.PI);
            RegisterVariable("e", Math.E);
            RegisterVariable("tau", Math.PI * 2.0);
            RegisterVariable("phi", (1.0 + Math.Sqrt(5.0)) / 2.0);
            RegisterVariable("inf", double.PositiveInfinity);
            RegisterVariable("nan", double.NaN);
            RegisterVariable("degToRad", Math.PI / 180.0);
            RegisterVariable("radToDeg", 180.0 / Math.PI);

            // Register functions
            RegisterFunction("sin", parameters => Math.Sin(parameters[0]));
            RegisterFunction("cos", parameters => Math.Cos(parameters[0]));
            RegisterFunction("tan", parameters => Math.Tan(parameters[0]));
            RegisterFunction("asin", parameters => Math.Asin(parameters[0]));
            RegisterFunction("acos", parameters => Math.Acos(parameters[0]));
            RegisterFunction("atan", parameters => Math.Atan(parameters[0]));
            RegisterFunction("atan2", parameters => Math.Atan2(parameters[0], parameters[1]));
            RegisterFunction("sinh", parameters => Math.Sinh(parameters[0]));
            RegisterFunction("cosh", parameters => Math.Cosh(parameters[0]));
            RegisterFunction("tanh", parameters => Math.Tanh(parameters[0]));
            RegisterFunction("sqrt", parameters => Math.Sqrt(parameters[0]));
            //RegisterFunction("cbrt", parameters => Math.Cbrt(parameters[0]));
            RegisterFunction("root", parameters => Math.Pow(parameters[0], 1.0 / parameters[1]));
            RegisterFunction("exp", parameters => Math.Exp(parameters[0]));
            RegisterFunction("abs", parameters => Math.Abs(parameters[0]));
            RegisterFunction("log", parameters => Math.Log(parameters[0]));
            RegisterFunction("log10", parameters => Math.Log10(parameters[0]));
            //RegisterFunction("log2", parameters => Math.Log2(parameters[0]));
            RegisterFunction("logn", parameters => Math.Log(parameters[0], parameters[1]));
            RegisterFunction("pow", parameters => Math.Pow(parameters[0], parameters[1]));
            RegisterFunction("mod", parameters => parameters[0] % parameters[1]);
            RegisterFunction("min", parameters => Math.Min(parameters[0], parameters[1]));
            RegisterFunction("max", parameters => Math.Max(parameters[0], parameters[1]));
            RegisterFunction("floor", parameters => Math.Floor(parameters[0]));
            RegisterFunction("ceil", parameters => Math.Ceiling(parameters[0]));
            RegisterFunction("round", parameters => Math.Round(parameters[0]));
            RegisterFunction("sign", parameters => Math.Sign(parameters[0]));
            RegisterFunction("clamp", parameters => RTMath.Clamp(parameters[0], parameters[1], parameters[2]));
            RegisterFunction("lerp", parameters => RTMath.Lerp(parameters[0], parameters[1], parameters[2]));
            RegisterFunction("inverseLerp", parameters => RTMath.InverseLerp(parameters[0], parameters[1], parameters[2]));
        }

        /// <summary>
        /// Registers a variable to this <see cref="EvaluationContext"/>.
        /// </summary>
        /// <param name="identifier">The variable's identifier.</param>
        /// <param name="value">The variable's value.</param>
        public void RegisterVariable(string identifier, double value)
        {
            variables.Add(identifier, value);
        }

        /// <summary>
        /// Registers a function to this <see cref="EvaluationContext"/>.
        /// </summary>
        /// <param name="identifier">The function's identifier.</param>
        /// <param name="function">The function.</param>
        public void RegisterFunction(string identifier, MathFunction function)
        {
            functions.Add(identifier, function);
        }

        public double GetVariable(string identifier)
        {
            if (variables.TryGetValue(identifier, out var value))
                return value;
            throw new EvaluationException($"Unknown variable: {identifier}");
        }

        public double CallFunction(string identifier, double[] parameters)
        {
            //if (functions.TryGetValue(identifier, out var function))
            //    return function(parameters);

            if (TryCallFunction(identifier, parameters, out double result))
                return result;

            throw new EvaluationException($"Unknown function: {identifier}");
        }

        public bool TryCallFunction(string identifier, double[] parameters, out double result)
        {
            if (functions.TryGetValue(identifier, out var function))
            {
                result = function(parameters);
                return true;
            }

            if (identifier.Contains("#"))
            {
                var split = identifier.Split('#');

                switch (split[0])
                {
                    case "findAxis": {
                            if (!CoreHelper.InGame)
                            {
                                result = 0;
                                return false;
                            }

                            var tag = split[1];

                            var bm = GameData.Current.FindObjectWithTag(tag);
                            if (!bm)
                            {
                                result = 0;
                                return false;
                            }

                            var fromType = (int)parameters[0];

                            if (fromType < 0 || fromType > 2)
                            {
                                result = 0;
                                return false;
                            }

                            var fromAxis = (int)parameters[1];

                            var time = parameters.Length < 3 ? RTLevel.Current.CurrentTime - bm.StartTime : (float)parameters[2];
                            var cachedSequences = bm.cachedSequences;

                            if (!cachedSequences)
                            {
                                result = 0;
                                return false;
                            }

                            result = fromType switch
                            {
                                0 => cachedSequences.PositionSequence.Interpolate(time)[fromAxis],
                                1 => cachedSequences.ScaleSequence.Interpolate(time)[fromAxis],
                                2 => cachedSequences.RotationSequence.Interpolate(time),
                                _ => 0,
                            };
                            return true;
                        }
                    case "findOffset": {
                            if (!CoreHelper.InGame)
                            {
                                result = 0;
                                return false;
                            }

                            var tag = split[1];

                            var bm = GameData.Current.FindObjectWithTag(tag);
                            if (!bm)
                            {
                                result = 0;
                                return false;
                            }

                            var fromType = (int)parameters[0];

                            if (fromType < 0 || fromType > 2)
                            {
                                result = 0;
                                return false;
                            }

                            var fromAxis = (int)parameters[1];

                            result = fromType switch
                            {
                                0 => bm.positionOffset[fromAxis],
                                1 => bm.scaleOffset[fromAxis],
                                2 => bm.rotationOffset[fromAxis],
                                _ => 0,
                            };
                            return true;
                        }
                    case "findObject": {
                            if (!CoreHelper.InGame)
                            {
                                result = 0;
                                return false;
                            }

                            var tag = split[1];

                            var bm = GameData.Current.FindObjectWithTag(tag);
                            if (!bm || split.Length < 2)
                            {
                                result = 0;
                                return false;
                            }

                            result = split[2] switch
                            {
                                "StartTime" => bm.StartTime,
                                "Depth" => bm.Depth,
                                "IntVariable" => bm.integerVariable,
                                "OriginX" => bm.origin.x,
                                "OriginY" => bm.origin.y,
                                _ => 0,
                            };
                            return true;
                        }
                    case "findInterpolateChain": {
                            if (!CoreHelper.InGame)
                            {
                                result = 0;
                                return false;
                            }

                            var tag = split[1];

                            var bm = GameData.Current.FindObjectWithTag(tag);
                            if (!bm)
                            {
                                result = 0;
                                return false;
                            }

                            var type = parameters[0];
                            var axis = parameters[1];
                            var hasValues = parameters.Length < 3;
                            var time = hasValues ? RTLevel.Current?.CurrentTime - bm.StartTime : (float)parameters[2];

                            result = type switch
                            {
                                0 => bm.InterpolateChainPosition((float)time, hasValues && parameters[3] == 1, !hasValues || parameters[4] == 1, !hasValues || parameters[5] == 1)[(int)axis],
                                1 => bm.InterpolateChainScale((float)time, !hasValues || parameters[3] == 1, !hasValues || parameters[4] == 1)[(int)axis],
                                2 => bm.InterpolateChainRotation((float)time, !hasValues || parameters[3] == 1, !hasValues || parameters[4] == 1),
                                _ => 0,
                            };
                            return true;
                        }
                    case "easing": {
                            result = Ease.GetEaseFunction(split[1])((float)parameters[0]);
                            return true;
                        }
                    case "date": {
                            result = float.Parse(DateTime.Now.ToString(split[1]));
                            return true;
                        }
                }
            }

            result = 0;
            return false;
        }

        /// <summary>
        /// Creates a default implementation of <see cref="IEvaluationContext"/>.
        /// </summary>
        /// <returns>The default <see cref="IEvaluationContext"/></returns>
        public static EvaluationContext CreateDefault()
        {
            var context = new EvaluationContext();
            context.RegisterBuiltIns();
            return context;
        }
    }
}