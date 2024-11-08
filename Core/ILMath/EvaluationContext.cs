using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
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
                    case "findAxis":
                        {
                            var tag = split[1];

                            var bm = CoreHelper.FindObjectWithTag(tag);
                            float value = 0f;
                            if (bm)
                            {
                                var fromType = (int)parameters[0];

                                if (fromType < 0 || fromType > 2)
                                {
                                    result = 0;
                                    return false;
                                }

                                var fromAxis = (int)parameters[1];

                                var time =(float)parameters[2];

                                if (!Updater.levelProcessor.converter.cachedSequences.TryGetValue(bm.id, out BetterLegacy.Core.Optimization.Objects.ObjectConverter.CachedSequences cachedSequence))
                                {
                                    result = 0;
                                    return false;
                                }

                                switch (fromType)
                                {
                                    case 0:
                                        {
                                            var sequence = cachedSequence.Position3DSequence.Interpolate(time);
                                            value = fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z;
                                            break;
                                        }
                                    case 1:
                                        {
                                            var sequence = cachedSequence.ScaleSequence.Interpolate(time);
                                            value = fromAxis == 0 ? sequence.x : sequence.y;
                                            break;
                                        }
                                    case 2:
                                        {
                                            value = cachedSequence.RotationSequence.Interpolate(time);
                                            break;
                                        }
                                }

                            }

                            result = value;
                            return true;
                        }
                    case "findOffset":
                        {
                            var tag = split[1];

                            var bm = CoreHelper.FindObjectWithTag(tag);
                            float value = 0f;
                            if (bm)
                            {
                                var fromType = (int)parameters[0];

                                if (fromType < 0 || fromType > 2)
                                {
                                    result = 0;
                                    return false;
                                }

                                var fromAxis = (int)parameters[1];

                                switch (fromType)
                                {
                                    case 0:
                                        {
                                            value = bm.positionOffset[fromAxis];
                                            break;
                                        }
                                    case 1:
                                        {
                                            value = bm.scaleOffset[fromAxis];
                                            break;
                                        }
                                    case 2:
                                        {
                                            value = bm.rotationOffset[fromAxis];
                                            break;
                                        }
                                }

                            }

                            result = value;
                            return true;
                        }
                    case "easing":
                        {
                            var easing = Ease.GetEaseFunction(split[1]);
                            result = easing((float)parameters[0]);
                            return true;
                        }
                    case "date":
                        {
                            var format = DateTime.Now.ToString(split[1]);
                            result = float.Parse(format);
                            return true;
                        }
                    case "findObject":
                        {
                            var tag = split[1];

                            var bm = CoreHelper.FindObjectWithTag(tag);
                            if (!bm || split.Length < 2)
                            {
                                result = 0;
                                return false;
                            }

                            float value = split[2] switch
                            {
                                "StartTime" => bm.StartTime,
                                "Depth" => bm.Depth,
                                "IntVariable" => bm.integerVariable,

                                _ => 0,
                            };

                            result = value;
                            return true;
                        }
                    case "findInterpolateChain":
                        {
                            var tag = split[1];

                            var bm = CoreHelper.FindObjectWithTag(tag);
                            if (!bm)
                            {
                                result = 0;
                                return false;
                            }

                            var type = parameters[0];
                            var axis = parameters[1];
                            var time = parameters[2];

                            result = type switch
                            {
                                0 => bm.InterpolateChainPosition((float)time, parameters[3] == 1, parameters[4] == 1, parameters[5] == 1)[(int)axis],
                                1 => bm.InterpolateChainScale((float)time, parameters[3] == 1, parameters[4] == 1)[(int)axis],
                                2 => bm.InterpolateChainRotation((float)time, parameters[3] == 1, parameters[4] == 1),
                                _ => 0,
                            };
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