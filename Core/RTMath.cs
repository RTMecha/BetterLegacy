using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Helpers;
using System.Text.RegularExpressions;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Optimization;
using ILMath;

namespace BetterLegacy.Core
{
    public static class RTMath
    {
        // from https://stackoverflow.com/questions/355062/is-there-a-string-math-evaluator-in-net
        public static double Evaluate(string str)
        {
            try
            {
                return Convert.ToDouble(new DataTable().Compute(str, null));
            }
            catch (Exception ex)
            {
                if (logException)
                    CoreHelper.LogError($"Error!\nMath: {str}\nException: {ex}");
                return 0;
            }
        }

        public static bool TryEvaluate(string str, out double result)
        {
            try
            {
                result = Convert.ToDouble(new DataTable().Compute(str, null));
                return true;
            }
            catch (Exception ex)
            {
                if (logException)
                    CoreHelper.LogError($"Error!\nMath: {str}\nException: {ex}");
                result = 0;
                return false;
            }
        }

        public static bool logException = false;

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
                var context = EvaluationContext.CreateDefault();

                //context.RegisterFunction("myFunction", parameters => parameters[0] + parameters[1]);

                if (variables != null)
                    foreach (var variable in variables)
                        context.RegisterVariable(variable.Key, variable.Value);

                context.RegisterVariable("deathCount", GameManager.inst.deaths.Count);
                context.RegisterVariable("hitCount", GameManager.inst.hits.Count);
                context.RegisterVariable("boostCount", LevelManager.BoostCount);
                context.RegisterVariable("actionMoveX", InputDataManager.inst.menuActions.Move.X);
                context.RegisterVariable("actionMoveY", InputDataManager.inst.menuActions.Move.Y);
                context.RegisterVariable("time", Time.time);
                context.RegisterVariable("deltaTime", Time.deltaTime);
                context.RegisterVariable("audioTime", AudioManager.inst.CurrentAudioSource.time);
                context.RegisterVariable("smoothedTime", RTEventManager.inst.currentTime);
                context.RegisterVariable("volume", AudioManager.inst.musicVol);
                context.RegisterVariable("pitch", AudioManager.inst.pitch);
                context.RegisterVariable("forwardPitch", CoreHelper.ForwardPitch);
                context.RegisterVariable("playerHealthTotal", PlayerManager.Players.Sum(x => x.Health));
                context.RegisterVariable("camPosX", EventManager.inst.cam.transform.position.x);
                context.RegisterVariable("camPosY", EventManager.inst.cam.transform.position.y);
                context.RegisterVariable("camZoom", EventManager.inst.cam.orthographicSize);
                context.RegisterVariable("camRot", EventManager.inst.cam.transform.localEulerAngles.z);
                context.RegisterVariable("mousePosX", Input.mousePosition.x);
                context.RegisterVariable("mousePosY", Input.mousePosition.y);
                context.RegisterVariable("screenHeight", Screen.height);
                context.RegisterVariable("screenWidth", Screen.width);

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
                    2 => RandomHelper.RandomInstanceSingle((int)parameters[0], (int)parameters[1]),
                    _ => 0
                });
                context.RegisterFunction("randomRange", parameters => parameters.Length switch
                {
                    3 => RandomHelper.RandomInstanceSingleRange((int)parameters[0], (float)parameters[1], (float)parameters[2], new System.Random().Next()),
                    4 => RandomHelper.RandomInstanceSingleRange((int)parameters[0], (float)parameters[1], (float)parameters[2], (int)parameters[3]),
                    _ => 0
                });
                context.RegisterFunction("randomInt", parameters => parameters.Length switch
                {
                    0 => new System.Random().Next(),
                    1 => new System.Random((int)parameters[0]).Next(),
                    2 => RandomHelper.RandomInstance((int)parameters[0], (int)parameters[1]),
                    _ => 0
                });
                context.RegisterFunction("randomRangeInt", parameters => parameters.Length switch
                {
                    3 => RandomHelper.RandomInstanceRange((int)parameters[0], (int)parameters[1], (int)parameters[2], new System.Random().Next()),
                    4 => RandomHelper.RandomInstanceRange((int)parameters[0], (int)parameters[1], (int)parameters[2], (int)parameters[3]),
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
                context.RegisterFunction("sampleAudio", parameters => Updater.GetSample((int)parameters[0], (float)parameters[1]));
                context.RegisterFunction("copyEvent", parameters => RTEventManager.inst.Interpolate((int)parameters[0], (int)parameters[1], (float)parameters[2]));

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

        /// <summary>
        /// Takes any user-written variables and functions, calculates them and replaces their strings.
        /// </summary>
        /// <param name="input">Input to replace.</param>
        /// <returns>Returns a calculated single.</returns>
        public static float OldParse(string input, Dictionary<string, float> variables = null)
        {
            if (string.IsNullOrEmpty(input))
                return 0f;

            input = input.Remove(";"); // we remove ; because of the old math parser requiring it, so people who made things with the math evaluators won't need to update

            var methodIndexer = new List<int>();
            var startMethods = new List<int>();
            var endMethods = new List<int>();

            int methodCount = 0;
            int index = 0;

            while (index < input.Length)
            {
                if (input[index] == '(')
                {
                    methodIndexer.Add(index);
                    methodCount++;
                }

                if (input[index] == ')')
                {
                    endMethods.Add(index);
                    methodCount--;
                    startMethods.Add(methodIndexer[methodCount]);
                    methodIndexer.RemoveAt(methodCount);
                }

                index++;
            }

            Predicate<char> predicate = x => char.IsLetter(x) || char.IsDigit(x);

            for (int i = 0; i < startMethods.Count; i++)
            {
                var result = input.Substring(startMethods[i], endMethods[i] - startMethods[i] + 1);
                int num = startMethods[i];
                string methodName = "";

                while (num > 0)
                {
                    num--;
                    if (!predicate(input[num]))
                        break;

                    methodName += input[num];
                }

                methodName = new string(methodName.Reverse().ToArray());
                var fullMethod = methodName + result;
                var startMethodIndex = startMethods[i] - methodName.Length;

                switch (methodName)
                {
                    case "sin":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"sin\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var calc = Mathf.Sin(ParseVariables(match.Groups[1].ToString().Trim(), variables)).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "cos":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"cos\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var calc = Mathf.Cos(ParseVariables(match.Groups[1].ToString().Trim(), variables)).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);

                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "atan":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"atan\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var calc = Mathf.Atan(ParseVariables(match.Groups[1].ToString().Trim(), variables)).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);

                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "tan":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"tan\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var calc = Mathf.Tan(ParseVariables(match.Groups[1].ToString().Trim(), variables)).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);

                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "asin":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"asin\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var calc = Mathf.Asin(ParseVariables(match.Groups[1].ToString().Trim(), variables)).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "acos":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"asin\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var calc = Mathf.Acos(ParseVariables(match.Groups[1].ToString().Trim(), variables)).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "sqrt":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"sqrt\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var calc = Mathf.Sqrt(ParseVariables(match.Groups[1].ToString().Trim(), variables)).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "abs":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"abs\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var calc = Mathf.Abs(ParseVariables(match.Groups[1].ToString().Trim(), variables)).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "min":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"abs\((.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var value = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var min = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var calc = Mathf.Min(value, min).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "max":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"max\((.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var value = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var max = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var calc = Mathf.Max(value, max).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "clamp":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"clamp\((.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var value = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var min = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var max = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var calc = Clamp(value, min, max).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "clampZero":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"clampZero\((.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var value = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var min = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var max = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var calc = ClampZero(value, min, max).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "pow":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"pow\((.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var f = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var p = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var calc = Mathf.Pow(f, p).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "exp":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"exp\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var power = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var calc = Mathf.Exp(power).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "log":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"log\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var f = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var calc = Mathf.Log(f).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "log10":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"log10\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var f = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var calc = Mathf.Log10(f).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "ceil":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"ceil\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var f = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var calc = Mathf.Ceil(f).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "floor":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"floor\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var f = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var calc = Mathf.Floor(f).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "round":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"round\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var f = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var calc = Mathf.Round(f).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "sign":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"sign\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var f = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var calc = Mathf.Sign(f).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "lerp":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"lerp\((.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var start = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var end = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var t = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var calc = Lerp(start, end, t).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "lerpAngle":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"lerpAngle\((.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var start = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var end = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var t = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var calc = Mathf.LerpAngle(start, end, t).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "inverseLerp":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"inverseLerp\((.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var start = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var end = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var t = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var calc = Mathf.InverseLerp(start, end, t).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "moveTowards":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"moveTowards\((.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var current = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var target = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var maxDelta = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var calc = Mathf.MoveTowards(current, target, maxDelta).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "moveTowardsAngle":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"moveTowardsAngle\((.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var current = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var target = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var maxDelta = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var calc = Mathf.MoveTowardsAngle(current, target, maxDelta).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "smoothStep":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"smoothStep\((.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var from = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var to = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var t = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var calc = Mathf.SmoothStep(from, to, t).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "gamma":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"gamma\((.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var value = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var absmax = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var gamma = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var calc = Mathf.Gamma(value, absmax, gamma).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "approximately":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"approximately\((.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var a = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var b = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var calc = Mathf.Approximately(a, b).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "repeat":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"repeat\((.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var t = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var length = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var calc = Mathf.Repeat(t, length).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "pingPong":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"pingPong\((.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var t = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var length = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var calc = Mathf.PingPong(t, length).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "deltaAngle":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"deltaAngle\((.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var current = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var target = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var calc = Mathf.DeltaAngle(current, target).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "random":
                        {

                            if (CoreHelper.RegexMatch(fullMethod, new Regex(@"random\((.*?),(.*?)\)"), out Match match1))
                            {
                                try
                                {
                                    var seed = (int)ParseVariables(match1.Groups[1].ToString().Trim(), variables);
                                    var seedIndex = (int)ParseVariables(match1.Groups[2].ToString().Trim(), variables);
                                    var calc = RandomHelper.RandomInstanceSingle(seed, seedIndex).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            }
                            else if (CoreHelper.RegexMatch(fullMethod, new Regex(@"random\((.*?)\)"), out Match match2))
                            {
                                try
                                {
                                    var seed = (int)ParseVariables(match1.Groups[1].ToString().Trim(), variables);
                                    var calc = ((float)new System.Random(seed).NextDouble()).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            }
                            else
                            {
                                var calc = ((float)new System.Random().NextDouble()).ToString();

                                input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);

                                //CoreHelper.Log($"calc: {calc}\n" +
                                //    $"fullMethod: {fullMethod}\n" +
                                //    $"input: {input}");
                            }

                            break;
                        }
                    case "randomRange":
                        {

                            if (CoreHelper.RegexMatch(fullMethod, new Regex(@"randomRange\((.*?),(.*?),(.*?)\)"), out Match match1))
                            {
                                try
                                {
                                    var seed = (int)ParseVariables(match1.Groups[1].ToString().Trim(), variables);
                                    var min = ParseVariables(match1.Groups[2].ToString().Trim(), variables);
                                    var max = ParseVariables(match1.Groups[3].ToString().Trim(), variables);

                                    var calc = RandomHelper.RandomInstanceSingleRange(seed, min, max, new System.Random().Next()).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            }
                            else if (CoreHelper.RegexMatch(fullMethod, new Regex(@"randomRange\((.*?),(.*?),(.*?),(.*?)\)"), out Match match2))
                            {
                                try
                                {
                                    var seed = (int)ParseVariables(match1.Groups[1].ToString().Trim(), variables);
                                    var min = ParseVariables(match1.Groups[2].ToString().Trim(), variables);
                                    var max = ParseVariables(match1.Groups[3].ToString().Trim(), variables);
                                    var seedIndex = (int)ParseVariables(match1.Groups[4].ToString().Trim(), variables);

                                    var calc = RandomHelper.RandomInstanceSingleRange(seed, min, max, seedIndex).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            }
                            else
                                input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);

                            break;
                        }
                    case "randomInt":
                        {

                            if (CoreHelper.RegexMatch(fullMethod, new Regex(@"random\((.*?),(.*?)\)"), out Match match1))
                            {
                                try
                                {
                                    var seed = (int)ParseVariables(match1.Groups[1].ToString().Trim(), variables);
                                    var seedIndex = (int)ParseVariables(match1.Groups[2].ToString().Trim(), variables);
                                    var calc = RandomHelper.RandomInstance(seed, seedIndex).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            }
                            else if (CoreHelper.RegexMatch(fullMethod, new Regex(@"random\((.*?)\)"), out Match match2))
                            {
                                try
                                {
                                    var seed = (int)ParseVariables(match1.Groups[1].ToString().Trim(), variables);
                                    var calc = ((float)new System.Random(seed).Next()).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            }
                            else
                            {
                                var calc = ((float)new System.Random().Next()).ToString();

                                input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                            }

                            break;
                        }
                    case "randomRangeInt":
                        {

                            if (CoreHelper.RegexMatch(fullMethod, new Regex(@"randomRangeInt\((.*?),(.*?),(.*?)\)"), out Match match1))
                            {
                                try
                                {
                                    var seed = (int)ParseVariables(match1.Groups[1].ToString().Trim(), variables);
                                    var min = (int)ParseVariables(match1.Groups[2].ToString().Trim(), variables);
                                    var max = (int)ParseVariables(match1.Groups[3].ToString().Trim(), variables);

                                    var calc = RandomHelper.RandomInstanceRange(seed, min, max, new System.Random().Next()).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            }
                            else if (CoreHelper.RegexMatch(fullMethod, new Regex(@"randomRangeInt\((.*?),(.*?),(.*?),(.*?)\)"), out Match match2))
                            {
                                try
                                {
                                    var seed = (int)ParseVariables(match1.Groups[1].ToString().Trim(), variables);
                                    var min = (int)ParseVariables(match1.Groups[2].ToString().Trim(), variables);
                                    var max = (int)ParseVariables(match1.Groups[3].ToString().Trim(), variables);
                                    var seedIndex = (int)ParseVariables(match1.Groups[4].ToString().Trim(), variables);

                                    var calc = RandomHelper.RandomInstanceRange(seed, min, max, seedIndex).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            }
                            else
                                input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);

                            break;
                        }
                    case "roundToNearestNumber":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"roundToNearestNumber\((.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var value = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var multipleOf = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var calc = RoundToNearestNumber(value, multipleOf).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "roundToNearestDecimal":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"roundToNearestDecimal\((.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var value = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var places = (int)ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var calc = RoundToNearestDecimal(value, places).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "percentage":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"percentage\((.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var t = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var length = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var calc = Percentage(t, length).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "equals":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"equals\((.*?),(.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var a = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var b = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var trueResult = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var falseResult = ParseVariables(match.Groups[4].ToString().Trim(), variables);

                                    var calc = (a == b ? trueResult : falseResult).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "lesserEquals":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"lesserEquals\((.*?),(.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var a = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var b = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var trueResult = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var falseResult = ParseVariables(match.Groups[4].ToString().Trim(), variables);

                                    var calc = (a <= b ? trueResult : falseResult).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "greaterEquals":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"greaterEquals\((.*?),(.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var a = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var b = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var trueResult = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var falseResult = ParseVariables(match.Groups[4].ToString().Trim(), variables);

                                    var calc = (a >= b ? trueResult : falseResult).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "lesser":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"lesser\((.*?),(.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var a = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var b = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var trueResult = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var falseResult = ParseVariables(match.Groups[4].ToString().Trim(), variables);

                                    var calc = (a < b ? trueResult : falseResult).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "greater":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"greater\((.*?),(.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var a = ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var b = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var trueResult = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var falseResult = ParseVariables(match.Groups[4].ToString().Trim(), variables);

                                    var calc = (a > b ? trueResult : falseResult).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "findAxis":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"findAxis\((.*?),(.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var tag = match.Groups[1].ToString().Trim();

                                    var bm = CoreHelper.FindObjectWithTag(tag);
                                    float value = 0f;
                                    if (bm)
                                    {
                                        var fromType = (int)ParseVariables(match.Groups[2].ToString().Trim(), variables);

                                        if (fromType < 0 || fromType > 2)
                                        {
                                            input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                            return;
                                        }

                                        var fromAxis = (int)ParseVariables(match.Groups[1].ToString().Trim(), variables);

                                        if (variables != null)
                                            variables["foundObjectStartTime"] = bm.StartTime;

                                        var time = ParseVariables(match.Groups[2].ToString().Trim(), variables ?? new Dictionary<string, float>
                                        {
                                            { "foundObjectStartTime", bm.StartTime }
                                        });

                                        if (!Updater.levelProcessor.converter.cachedSequences.TryGetValue(bm.id, out Optimization.Objects.ObjectConverter.CachedSequences cachedSequence))
                                        {
                                            input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                            return;
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

                                    input = UpdateInput(input, i, value.ToString(), fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "easing":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"easing\((.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var curveType = Ease.GetEaseFunction(match.Groups[1].ToString().Trim());
                                    var x = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var calc = curveType(x).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "int":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"int\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var x = (int)ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var calc = x.ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "date":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"date\((.*?)\)"), match =>
                            {
                                try
                                {
                                    var calc = DateTime.Now.ToString(match.Groups[1].ToString().Trim());

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "sampleAudio":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"sampleAudio\((.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var sample = (int)ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var intensity = ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var calc = Updater.GetSample(sample, intensity).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                    case "copyEvent":
                        {
                            CoreHelper.RegexMatch(fullMethod, new Regex(@"copyEvent\((.*?),(.*?),(.*?)\)"), match =>
                            {
                                try
                                {
                                    var type = (int)ParseVariables(match.Groups[1].ToString().Trim(), variables);
                                    var valueIndex = (int)ParseVariables(match.Groups[2].ToString().Trim(), variables);
                                    var time = ParseVariables(match.Groups[3].ToString().Trim(), variables);
                                    var calc = RTEventManager.inst.Interpolate(type, valueIndex, time).ToString();

                                    input = UpdateInput(input, i, calc, fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                                catch
                                {
                                    input = UpdateInput(input, i, "0", fullMethod, startMethodIndex, startMethods, endMethods);
                                }
                            });

                            break;
                        }
                }
            }

            methodIndexer.Clear();
            startMethods.Clear();
            endMethods.Clear();
            methodIndexer = null;
            startMethods = null;
            endMethods = null;

            return ParseVariables(input, variables);
        }

        /// <summary>
        /// Checks if a character is a math symbol.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>Returns true if the character is a math symbol, otherwise returns false.</returns>
        public static bool CharacterIsMathSymbol(char c) => c == '+' || c == '-' || c == '/' || c == '%' || c == '*';

        static string UpdateInput(string input, int index, string calc, string fullMethod, int startMethodIndex, List<int> startMethods, List<int> endMethods)
        {
            input = CoreHelper.ReplaceInsert(input, calc, startMethodIndex, endMethods[index]);

            UpdateFunctionLengths(index, fullMethod, calc, startMethods, endMethods);

            return input;
        }

        static void UpdateFunctionLengths(int index, string fullMethod, string calc, List<int> startMethods, List<int> endMethods)
        {
            var a = fullMethod.Length;
            var b = calc.Length;

            for (int j = index + 1; j < startMethods.Count; j++)
            {
                if (startMethods[j] > startMethods[index])
                {
                    startMethods[j] -= a;
                    startMethods[j] += b;
                }

                if (endMethods[j] > endMethods[index])
                {
                    endMethods[j] -= a;
                    endMethods[j] += b;
                }
            }
        }

        // RTMath.Parse("(1 + 1) * 1");
        public static float ParseVariables(string input, Dictionary<string, float> variables = null)
            => string.IsNullOrEmpty(input) ? 0f : (float)Evaluate(ParseVariablesToString(input, variables));

        // RTMath.ParseVariablesToString("(1 + pitch");
        // RTMath.ParseVariablesToString("(1+1)*1")
        public static string ParseVariablesToString(string input, Dictionary<string, float> variables = null)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            input = input.Remove(" ");

            int search = 0;
            int startIndex = 0;
            bool isInVariable = false;
            bool lastSymbolWasClosing = false;
            while (search < input.Length)
            {
                var c = input[search];

                // we only need to check if the character is a math symbol once the variable has been declared, so we can consider negative numbers.
                if (char.IsDigit(c) || char.IsLetter(c) || c == '_' || c == '[' || c == ']' || c == '{' || c == '}')
                    isInVariable = true;

                if (c == '(')
                {
                    search++;
                    startIndex = search;
                    isInVariable = false;
                    continue;
                }

                var endOfInput = search >= input.Length - 1;

                // there aren't any cases of a variable being right next to a closing bracket ) so we skip.
                if (lastSymbolWasClosing || !isInVariable && (CharacterIsMathSymbol(c) || c == ')'))
                {
                    lastSymbolWasClosing = false;

                    search++;
                    startIndex = search;
                    isInVariable = false;
                    continue;
                }

                if (c == ')')
                    lastSymbolWasClosing = true;

                if (isInVariable && (CharacterIsMathSymbol(c) || lastSymbolWasClosing) || endOfInput)
                {
                    isInVariable = false;
                    var substring = input.Substring(startIndex, search - startIndex + (endOfInput && c != ')' ? 1 : 0));
                    var length = substring.Length;

                    //CoreHelper.Log($"substring: {substring}");

                    int minusCount = 0;
                    while (substring.Length > 0 && substring[0] == '-')
                    {
                        substring = substring.Substring(1, substring.Length - 1);
                        minusCount++;
                    }

                    var variable = ParseVariable(substring, variables);

                    // -pitch
                    // remove - and set minusCount to 1 since there was only 1 minus
                    // minusCount = 1
                    // minusCount > 0 = true
                    // variable = -variable
                    // minusCount--
                    // minusCount = 0
                    // minusCount > 0 = false

                    while (minusCount > 0)
                    {
                        variable = -variable;
                        minusCount--;
                    }

                    substring = variable.ToString();

                    input = CoreHelper.ReplaceInsert(input, substring, startIndex, endOfInput && c != ')' ? search : search - 1);

                    if (!endOfInput)
                    {
                        search -= length;
                        search += substring.Length;

                        startIndex = search + 1;
                    }
                }

                search++;
            }

            return input;
        }

        public static float ParseVariable(string name, Dictionary<string, float> variables = null)
        {
            //CoreHelper.Log($"Name: \"{name}\"\nvariables == null {variables == null}");

            switch (name)
            {
                case "deathCount":  return GameManager.inst.deaths.Count;
                case "hitCount":  return GameManager.inst.hits.Count;
                case "boostCount":  return LevelManager.BoostCount;
                case "actionMoveX":  return InputDataManager.inst.menuActions.Move.X;
                case "actionMoveY":  return InputDataManager.inst.menuActions.Move.Y;
                case "time":  return Time.time;
                case "deltaTime":  return Time.deltaTime;
                case "audioTime":  return AudioManager.inst.CurrentAudioSource.time;
                case "smoothedTime":  return RTEventManager.inst.currentTime;
                case "volume":  return AudioManager.inst.musicVol;
                case "pitch":  return AudioManager.inst.pitch;
                case "forwardPitch":  return CoreHelper.ForwardPitch;
                case "playerHealthTotal":  return PlayerManager.Players.Sum(x => x.Health);
                case "camPosX": return EventManager.inst.cam.transform.position.x;
                case "camPosY": return EventManager.inst.cam.transform.position.y;
                case "camZoom": return EventManager.inst.cam.orthographicSize;
                case "camRot": return EventManager.inst.cam.transform.localEulerAngles.z;
            }

            float output = 0f;
            bool set = false;
            CoreHelper.RegexMatch(name, new Regex(@"player([0-9]+)PosX"), match =>
            {
                var index = Mathf.Clamp(Parser.TryParse(match.Groups[1].ToString(), 0), 0, int.MaxValue);

                var players = PlayerManager.Players;
                if (players.Count <= index || !players[index].Player || !players[index].Player.rb)
                    output = 0f;
                else
                    output = players[index].Player.rb.position.x;
                set = true;
            });
            CoreHelper.RegexMatch(name, new Regex(@"player([0-9]+)PosY"), match =>
            {
                var index = Mathf.Clamp(Parser.TryParse(match.Groups[1].ToString(), 0), 0, int.MaxValue);

                var players = PlayerManager.Players;
                if (players.Count <= index || !players[index].Player || !players[index].Player.rb)
                    output = 0f;
                else
                    output = players[index].Player.rb.position.y;
                set = true;
            });
            CoreHelper.RegexMatch(name, new Regex(@"player([0-9]+)Health"), match =>
            {
                var index = Mathf.Clamp(Parser.TryParse(match.Groups[1].ToString(), 0), 0, int.MaxValue);

                var players = PlayerManager.Players;
                if (players.Count <= index)
                    output = 0f;
                else
                    output = players[index].Health;
                set = true;
            });

            if (set)
                return output;

            if (variables != null && variables.TryGetValue(name, out float value))
            {
                //CoreHelper.Log($"Name: \"{name}\"\nValue: {value}");
                return value;
            }

            if (TryEvaluate(name, out double result))
            {
                //CoreHelper.Log($"Name: \"{name}\"\nResult: {result}");
                return (float)result;
            }

            return 0f;
        }

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

        public static Vector2 Multiply(Vector2 a, Vector2 b) => new Vector2
        {
            x = a.x * b.x,
            y = a.y * b.y,
        };
        
        public static Vector3 Multiply(Vector3 a, Vector3 b) => new Vector3
        {
            x = a.x * b.x,
            y = a.y * b.y,
            z = a.z * b.z,
        };

        public static float RecursiveLerp(float t, float count)
        {
            return 0f;
        }

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
