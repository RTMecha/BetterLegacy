using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using ILMath.Exception;
using System;
using System.Collections.Generic;
using UnityEngine;

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
            RegisterVariable("True", 1.0);
            RegisterVariable("true", 1.0);
            RegisterVariable("False", 0.0);
            RegisterVariable("false", 0.0);

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


            RegisterFunction("clampZero", parameters => RTMath.ClampZero(parameters[0], parameters[1], parameters[2]));
            RegisterFunction("lerpAngle", parameters => Mathf.LerpAngle((float)parameters[0], (float)parameters[1], (float)parameters[2]));
            RegisterFunction("moveTowards", parameters => Mathf.MoveTowards((float)parameters[0], (float)parameters[1], (float)parameters[2]));
            RegisterFunction("moveTowardsAngle", parameters => Mathf.MoveTowardsAngle((float)parameters[0], (float)parameters[1], (float)parameters[2]));
            RegisterFunction("smoothStep", parameters => Mathf.SmoothStep((float)parameters[0], (float)parameters[1], (float)parameters[2]));
            RegisterFunction("gamma", parameters => Mathf.Gamma((float)parameters[0], (float)parameters[1], (float)parameters[2]));
            RegisterFunction("repeat", parameters => Mathf.Repeat((float)parameters[0], (float)parameters[1]));
            RegisterFunction("pingPong", parameters => Mathf.PingPong((float)parameters[0], (float)parameters[1]));
            RegisterFunction("deltaAngle", parameters => Mathf.DeltaAngle((float)parameters[0], (float)parameters[1]));
            RegisterFunction("random", parameters => parameters.Length switch
            {
                0 => new System.Random().NextDouble(),
                1 => new System.Random((int)parameters[0]).NextDouble(),
                2 => RandomHelper.SingleFromIndex(parameters[0].ToString(), (int)parameters[1]),
                _ => 0
            });
            RegisterFunction("randomSeed", parameters => parameters.Length switch
            {
                0 => new System.Random(RandomHelper.CurrentSeed.GetHashCode()).NextDouble(),
                1 => new System.Random((int)parameters[0] ^ RandomHelper.CurrentSeed.GetHashCode()).NextDouble(),
                _ => 0
            });
            RegisterFunction("randomRange", parameters => parameters.Length switch
            {
                2 => RandomHelper.SingleFromRange(new System.Random().Next().ToString(), (float)parameters[0], (float)parameters[1]),
                3 => RandomHelper.SingleFromIndexRange(parameters[0].ToString(), new System.Random().Next(), (float)parameters[1], (float)parameters[2]),
                4 => RandomHelper.SingleFromIndexRange(parameters[0].ToString(), (int)parameters[1], (float)parameters[2], (float)parameters[3]),
                _ => 0
            });
            RegisterFunction("randomSeedRange", parameters => parameters.Length switch
            {
                2 => RandomHelper.SingleFromRange(RandomHelper.CurrentSeed, (float)parameters[0], (float)parameters[1]),
                3 => RandomHelper.SingleFromIndexRange(((int)parameters[0] ^ RandomHelper.CurrentSeed.GetHashCode()).ToString(), new System.Random().Next(), (float)parameters[1], (float)parameters[2]),
                4 => RandomHelper.SingleFromIndexRange(((int)parameters[0] ^ RandomHelper.CurrentSeed.GetHashCode()).ToString(), (int)parameters[1], (float)parameters[2], (float)parameters[3]),
                _ => 0
            });
            RegisterFunction("randomInt", parameters => parameters.Length switch
            {
                0 => new System.Random().Next(),
                1 => new System.Random((int)parameters[0]).Next(),
                2 => RandomHelper.FromIndex(parameters[0].ToString(), (int)parameters[1]),
                _ => 0
            });
            RegisterFunction("randomSeedInt", parameters => parameters.Length switch
            {
                0 => new System.Random(RandomHelper.CurrentSeed.GetHashCode()).Next(),
                1 => new System.Random((int)parameters[0] ^ RandomHelper.CurrentSeed.GetHashCode()).Next(),
                _ => 0
            });
            RegisterFunction("randomRangeInt", parameters => parameters.Length switch
            {
                2 => RandomHelper.IntFromRange(new System.Random().Next().ToString(), (int)parameters[0], (int)parameters[1]),
                3 => RandomHelper.FromIndexRange(parameters[0].ToString(), new System.Random().Next(), (int)parameters[1], (int)parameters[2]),
                4 => RandomHelper.FromIndexRange(parameters[0].ToString(), (int)parameters[1], (int)parameters[2], (int)parameters[3]),
                _ => 0
            });
            RegisterFunction("randomSeedRangeInt", parameters => parameters.Length switch
            {
                2 => RandomHelper.IntFromRange(RandomHelper.CurrentSeed, (int)parameters[0], (int)parameters[1]),
                3 => RandomHelper.FromIndexRange(((int)parameters[0] ^ RandomHelper.CurrentSeed.GetHashCode()).ToString(), new System.Random().Next(), (int)parameters[1], (int)parameters[2]),
                4 => RandomHelper.FromIndexRange(((int)parameters[0] ^ RandomHelper.CurrentSeed.GetHashCode()).ToString(), (int)parameters[1], (int)parameters[2], (int)parameters[3]),
                _ => 0
            });
            RegisterFunction("roundToNearestNumber", parameters => RTMath.RoundToNearestNumber((float)parameters[0], (float)parameters[1]));
            RegisterFunction("roundToNearestDecimal", parameters => RTMath.RoundToNearestDecimal((float)parameters[0], (int)parameters[1]));
            RegisterFunction("percentage", parameters => RTMath.Percentage((float)parameters[0], (float)parameters[1]));
            RegisterFunction("equals", parameters => parameters[0] == parameters[1] ? parameters[2] : parameters[3]);
            RegisterFunction("lesserEquals", parameters => parameters[0] <= parameters[1] ? parameters[2] : parameters[3]);
            RegisterFunction("greaterEquals", parameters => parameters[0] >= parameters[1] ? parameters[2] : parameters[3]);
            RegisterFunction("lesser", parameters => parameters[0] < parameters[1] ? parameters[2] : parameters[3]);
            RegisterFunction("greater", parameters => parameters[0] > parameters[1] ? parameters[2] : parameters[3]);
            RegisterFunction("int", parameters => (int)parameters[0]);
            RegisterFunction("vectorAngle", parameters => RTMath.VectorAngle(new Vector3((float)parameters[0], (float)parameters[1], (float)parameters[2]), new Vector3((float)parameters[3], (float)parameters[4], (float)parameters[5])));
            RegisterFunction("distance", parameters => parameters.Length switch
            {
                2 => RTMath.Distance((float)parameters[0], (float)parameters[1]),
                4 => Vector2.Distance(new Vector2((float)parameters[0], (float)parameters[1]), new Vector2((float)parameters[2], (float)parameters[3])),
                6 => Vector3.Distance(new Vector3((float)parameters[0], (float)parameters[1], (float)parameters[2]), new Vector3((float)parameters[3], (float)parameters[4], (float)parameters[5])),
                _ => 0
            });
            RegisterFunction("worldToViewportPointX", parameters =>
            {
                var position = Camera.main.WorldToViewportPoint(new Vector3((float)parameters[0], parameters.Length > 1 ? (float)parameters[1] : 0f, parameters.Length > 2 ? (float)parameters[2] : 0f));
                return position.x;
            });
            RegisterFunction("worldToViewportPointY", parameters =>
            {
                var position = Camera.main.WorldToViewportPoint(new Vector3((float)parameters[0], parameters.Length > 1 ? (float)parameters[1] : 0f, parameters.Length > 2 ? (float)parameters[2] : 0f));
                return position.y;
            });
            RegisterFunction("worldToViewportPointZ", parameters =>
            {
                var position = Camera.main.WorldToViewportPoint(new Vector3((float)parameters[0], parameters.Length > 1 ? (float)parameters[1] : 0f, parameters.Length > 2 ? (float)parameters[2] : 0f));
                return position.z;
            });
            RegisterFunction("mirrorNegative", parameters => parameters[0] < 0 ? -parameters[0] : parameters[0]);
            RegisterFunction("mirrorPositive", parameters => parameters[0] > 0 ? -parameters[0] : parameters[0]);
            RegisterFunction("rotatePosX", parameters => RTMath.Rotate(new Vector2((float)parameters[0], (float)parameters[1]), (float)parameters[2]).x);
            RegisterFunction("rotatePosY", parameters => RTMath.Rotate(new Vector2((float)parameters[0], (float)parameters[1]), (float)parameters[2]).y);
        }

        /// <summary>
        /// Registers a variable to this <see cref="EvaluationContext"/>.
        /// </summary>
        /// <param name="identifier">The variable's identifier.</param>
        /// <param name="value">The variable's value.</param>
        public void RegisterVariable(string identifier, double value)
        {
            variables[identifier] = value;
        }

        /// <summary>
        /// Registers a function to this <see cref="EvaluationContext"/>.
        /// </summary>
        /// <param name="identifier">The function's identifier.</param>
        /// <param name="function">The function.</param>
        public void RegisterFunction(string identifier, MathFunction function)
        {
            functions[identifier] = function;
        }

        public void RegisterVariables(Dictionary<string, float> variables)
        {
            if (variables != null)
                foreach (var variable in variables)
                    RegisterVariable(variable.Key, variable.Value);
        }
        
        public void RegisterVariables(Dictionary<string, double> variables)
        {
            if (variables != null)
                foreach (var variable in variables)
                    RegisterVariable(variable.Key, variable.Value);
        }
        
        public void RegisterFunctions(Dictionary<string, MathFunction> functions)
        {
            if (functions != null)
                foreach (var function in functions)
                    RegisterFunction(function.Key, function.Value);
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
                            if (!ProjectArrhythmia.State.InGame)
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
                            if (!ProjectArrhythmia.State.InGame)
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
                            if (!ProjectArrhythmia.State.InGame)
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
                            if (!ProjectArrhythmia.State.InGame)
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

        public EvaluationContext Copy()
        {
            var context = new EvaluationContext();
            context.RegisterVariables(variables);
            context.RegisterFunctions(functions);
            return context;
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