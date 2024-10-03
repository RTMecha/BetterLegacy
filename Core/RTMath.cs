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
                if (logException1)
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
                if (logException1)
                    CoreHelper.LogError($"Error!\nMath: {str}\nException: {ex}");
                result = 0;
                return false;
            }
        }

        public static bool logException1 = false;
        public static bool logException2 = false;
        public static bool logException3 = false;

        static string GetFunctionPattern(string p, bool includeEnd) => p + (includeEnd ? ";" : "") + "";

        public static bool TryEvaluate(string input, float current, out float result)
        {
            try
            {
                var a = TryEvaluate(Replace(input.Replace("c", current.ToString())), out double b);
                result = a ? (float)b : current;
                return a;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Couldn't evaluate math.{ex}");
                result = current;
                return false;
            }
        }

        /// <summary>
        /// Takes any user-written variables and functions, calculates them and replaces their strings.
        /// </summary>
        /// <param name="input">Input to replace.</param>
        /// <param name="includeEnd">If each function needs to be capped with a ;.</param>
        /// <returns>Returns a calculated string.</returns>
        public static string Replace(string input, bool includeEnd = true)
        {
            try
            {
                input = input
                    .Replace("deathCount", GameManager.inst.deaths.Count.ToString())
                    .Replace("hitCount", GameManager.inst.hits.Count.ToString())
                    .Replace("boostCount", LevelManager.BoostCount.ToString())
                    .Replace("actionMoveX", InputDataManager.inst.menuActions.Move.X.ToString())
                    .Replace("actionMoveY", InputDataManager.inst.menuActions.Move.Y.ToString())
                    .Replace("time", Time.time.ToString())
                    .Replace("deltaTime", Time.deltaTime.ToString())
                    .Replace("audioTime", AudioManager.inst.CurrentAudioSource.time.ToString())
                    .Replace("volume", AudioManager.inst.musicVol.ToString())
                    .Replace("pitch", AudioManager.inst.pitch.ToString());

                try
                {
                    if (input.Contains("cam"))
                    {
                        if (input.Contains("camPosX"))
                            input = input.Replace("camPosX", EventManager.inst.cam.transform.position.x.ToString());
                        if (input.Contains("camPosY"))
                            input = input.Replace("camPosY", EventManager.inst.cam.transform.position.y.ToString());
                        if (input.Contains("camZoom"))
                            input = input.Replace("camZoom", EventManager.inst.cam.orthographicSize.ToString());
                        if (input.Contains("camRot"))
                            input = input.Replace("camRot", EventManager.inst.cam.transform.localEulerAngles.z.ToString());
                    }

                    if (input.Contains("player"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(@"player([0-9]+)PosX"), match =>
                        {
                            var baseString = match.Groups[0].ToString();
                            var index = Mathf.Clamp(Parser.TryParse(match.Groups[1].ToString(), 0), 0, int.MaxValue);

                            if (PlayerManager.Players.Count <= index || !PlayerManager.Players[index].Player || !PlayerManager.Players[index].Player.rb)
                                input = input.Replace(baseString, "0");
                            else
                                input = input.Replace(baseString, PlayerManager.Players[index].Player.rb.position.x.ToString());
                        });

                        CoreHelper.RegexMatches(input, new Regex(@"player([0-9]+)PosY"), match =>
                        {
                            var baseString = match.Groups[0].ToString();
                            var index = Mathf.Clamp(Parser.TryParse(match.Groups[1].ToString(), 0), 0, int.MaxValue);

                            if (PlayerManager.Players.Count <= index || !PlayerManager.Players[index].Player || !PlayerManager.Players[index].Player.rb)
                                input = input.Replace(baseString, "0");
                            else
                                input = input.Replace(baseString, PlayerManager.Players[index].Player.rb.position.y.ToString());
                        });

                        CoreHelper.RegexMatches(input, new Regex(@"player([0-9]+)Health"), match =>
                        {
                            var baseString = match.Groups[0].ToString();
                            var index = Mathf.Clamp(Parser.TryParse(match.Groups[1].ToString(), 0), 0, int.MaxValue);

                            if (PlayerManager.Players.Count <= index)
                                input = input.Replace(baseString, "0");
                            else
                                input = input.Replace(baseString, PlayerManager.Players[index].Health.ToString());
                        });

                        if (input.Contains("playerHealthTotal"))
                            input = input.Replace("playerHealthTotal", PlayerManager.Players.Sum(x => x.Health).ToString());

                        if (input.Contains("playerTailPosX"))
                        {
                            CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"playerTailPosX\((.*?),(.*?)\)", includeEnd)), match =>
                            {
                                try
                                {
                                    var baseString = match.Groups[0].ToString();
                                    var index = Mathf.Clamp((int)Evaluate(Replace(match.Groups[1].ToString().Trim(), false)), 0, int.MaxValue);
                                    var tailIndex = Mathf.Clamp((int)Evaluate(Replace(match.Groups[2].ToString().Trim(), false)), 0, int.MaxValue);

                                    if (PlayerManager.Players.Count <= index || !PlayerManager.Players[index].Player || !PlayerManager.Players[index].Player.rb)
                                        input = input.Replace(baseString, "0");
                                    else
                                        input = input.Replace(baseString, PlayerManager.Players[index].Player.tailParts[tailIndex].Transform.position.x.ToString());
                                }
                                catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                            });
                        }

                        if (input.Contains("playerTailPosY"))
                        {
                            CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"playerTailPosY\((.*?),(.*?)\)", includeEnd)), match =>
                            {
                                try
                                {
                                    var baseString = match.Groups[0].ToString();
                                    var index = Mathf.Clamp((int)Evaluate(Replace(match.Groups[1].ToString().Trim(), false)), 0, int.MaxValue);
                                    var tailIndex = Mathf.Clamp((int)Evaluate(Replace(match.Groups[2].ToString().Trim(), false)), 0, int.MaxValue);

                                    if (PlayerManager.Players.Count <= index || !PlayerManager.Players[index].Player || !PlayerManager.Players[index].Player.rb)
                                        input = input.Replace(baseString, "0");
                                    else
                                        input = input.Replace(baseString, PlayerManager.Players[index].Player.tailParts[tailIndex].Transform.position.y.ToString());
                                }
                                catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                            });
                        }
                    }

                    // math functions

                    if (input.Contains("sin"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"sin\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Sin((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("cos"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"cos\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Cos((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("atan"))
                    {
                        
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"atan\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Atan((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("tan"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"tan\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Tan((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("asin"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"asin\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Asin((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("acos"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"acos\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Acos((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("sqrt"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"sqrt\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Sqrt((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("abs"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"abs\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Abs((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("min"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"min\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var a = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var b = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Min(a, b).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("max"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"max\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var a = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var b = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Max(a, b).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("clamp"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"clamp\((.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var a = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var b = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var c = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));
                                input = input.Replace(match.Groups[0].ToString(), Clamp(a, b, c).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("clampZero"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"clampZero\((.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var a = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var b = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var c = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));
                                input = input.Replace(match.Groups[0].ToString(), ClampZero(a, b, c).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("pow"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"pow\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var a = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var b = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Pow(a, b).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("exp"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"exp\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Exp((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("log"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"log\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Log((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"log\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var a = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var b = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Log(a, b).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("log10"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"log10\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Log10((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("ceil"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"ceil\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Ceil((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("floor"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"floor\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Floor((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("round"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"round\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Round((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("sign"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"sign\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Mathf.Sign((float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("lerp"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"lerp\((.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var x = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var y = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var t = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), Lerp(x, y, t).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("lerpAngle"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"lerpAngle\((.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var x = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var y = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var t = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), Mathf.LerpAngle(x, y, t).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("inverseLerp"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"inverseLerp\((.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var x = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var y = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var t = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), Mathf.InverseLerp(x, y, t).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("moveTowards"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"moveTowards\((.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var x = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var y = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var t = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), Mathf.MoveTowards(x, y, t).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("moveTowardsAngle"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"moveTowardsAngle\((.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var x = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var y = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var t = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), Mathf.MoveTowardsAngle(x, y, t).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("smoothStep"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"smoothStep\((.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var x = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var y = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var t = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), Mathf.SmoothStep(x, y, t).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("gamma"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"gamma\((.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var x = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var y = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var t = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), Mathf.Gamma(x, y, t).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("approximately"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"approximately\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var x = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var y = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), Mathf.Approximately(x, y).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("repeat"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"repeat\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var x = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var y = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), Mathf.Repeat(x, y).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("pingPong"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"pingPong\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var x = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var y = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), Mathf.PingPong(x, y).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("deltaAngle"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"deltaAngle\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var x = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var y = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), Mathf.DeltaAngle(x, y).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("random"))
                    {
                        input = input.Replace("random()", ((float)new System.Random().NextDouble()).ToString());
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"random\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var seed = (int)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), ((float)(new System.Random(seed).NextDouble())).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"random\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var seed = (int)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var index = (int)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), RandomHelper.RandomInstanceSingle(seed, index).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("randomRange"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"randomRange\((.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var seed = (int)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var min = (int)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var max = (int)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), RandomHelper.RandomInstanceSingleRange(seed, min, max, new System.Random().Next()).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"randomRange\((.*?),(.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var seed = (int)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var min = (int)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var max = (int)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));
                                var index = (int)Evaluate(Replace(match.Groups[4].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), RandomHelper.RandomInstanceSingleRange(seed, min, max, index).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("randomInt"))
                    {
                        input = input.Replace("randomInt()", new System.Random().Next().ToString());
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"randomInt\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var seed = (int)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), new System.Random(seed).Next().ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"randomInt\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var seed = (int)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var index = (int)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), RandomHelper.RandomInstance(seed, index).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("randomRangeInt"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"randomRangeInt\((.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var seed = (int)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var min = (int)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var max = (int)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), RandomHelper.RandomInstanceRange(seed, min, max, new System.Random().Next()).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"randomRangeInt\((.*?),(.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var seed = (int)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var min = (int)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var max = (int)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));
                                var index = (int)Evaluate(Replace(match.Groups[4].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), RandomHelper.RandomInstanceRange(seed, min, max, index).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("roundToNearestNumber"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"roundToNearestNumber\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var value = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var multipleOf = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), RoundToNearestNumber(value, multipleOf).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("roundToNearestDecimal"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"roundToNearestDecimal\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var value = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var places = (int)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), RoundToNearestDecimal(value, places).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("percentage"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"percentage\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var t = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var length = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), Percentage(t, length).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("equals"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"equals\((.*?),(.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var a = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var b = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var a2 = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));
                                var b2 = (float)Evaluate(Replace(match.Groups[4].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), (a == b ? a2 : b2).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }
                    
                    if (input.Contains("lesserEquals"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"lesserEquals\((.*?),(.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var a = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var b = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var a2 = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));
                                var b2 = (float)Evaluate(Replace(match.Groups[4].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), (a <= b ? a2 : b2).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }
                    
                    if (input.Contains("greaterEquals"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"greaterEquals\((.*?),(.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var a = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var b = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var a2 = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));
                                var b2 = (float)Evaluate(Replace(match.Groups[4].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), (a >= b ? a2 : b2).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }
                    
                    if (input.Contains("lesser"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"lesser\((.*?),(.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var a = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var b = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var a2 = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));
                                var b2 = (float)Evaluate(Replace(match.Groups[4].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), (a < b ? a2 : b2).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }
                    
                    if (input.Contains("greater"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"greater\((.*?),(.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var a = (float)Evaluate(Replace(match.Groups[1].ToString().Trim(), false));
                                var b = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));
                                var a2 = (float)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));
                                var b2 = (float)Evaluate(Replace(match.Groups[4].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), (a > b ? a2 : b2).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }
                    
                    if (input.Contains("findAxis"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"findAxis\((.*?),(.*?),(.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var tag = match.Groups[1].ToString().Trim();

                                var bm = CoreHelper.FindObjectWithTag(tag);
                                if (bm)
                                {
                                    var fromType = (int)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));

                                    if (fromType < 0 || fromType > 2)
                                    {
                                        input = input.Replace(match.Groups[0].ToString(), "0");
                                        return;
                                    }

                                    var fromAxis = (int)Evaluate(Replace(match.Groups[3].ToString().Trim(), false));
                                    var time = (float)Evaluate(Replace(match.Groups[4].ToString().Trim(), false));
                                    float value = 0f;

                                    if (!Optimization.Updater.levelProcessor.converter.cachedSequences.TryGetValue(bm.id, out Optimization.Objects.ObjectConverter.CachedSequences cachedSequence))
                                    {
                                        input = input.Replace(match.Groups[0].ToString(), "0");
                                        return;
                                    }

                                    switch (fromType)
                                    {
                                        case 0:
                                            {
                                                var sequence = cachedSequence.Position3DSequence.Interpolate(time - bm.StartTime);
                                                value = fromAxis == 0 ? sequence.x : fromAxis == 1 ? sequence.y : sequence.z;
                                                break;
                                            }
                                        case 1:
                                            {
                                                var sequence = cachedSequence.ScaleSequence.Interpolate(time - bm.StartTime);
                                                value = fromAxis == 0 ? sequence.x : sequence.y;
                                                break;
                                            }
                                        case 2:
                                            {
                                                value = cachedSequence.RotationSequence.Interpolate(time - bm.StartTime);
                                                break;
                                            }
                                    }

                                    input = input.Replace(match.Groups[0].ToString(), value.ToString());
                                }
                                else
                                {
                                    input = input.Replace(match.Groups[0].ToString(), "0");
                                }
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("easing"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"easing\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                var curveType = Ease.GetEaseFunction(match.Groups[1].ToString().Trim());
                                var x = (float)Evaluate(Replace(match.Groups[2].ToString().Trim(), false));

                                input = input.Replace(match.Groups[0].ToString(), curveType(x).ToString());
                            }
                            catch { input = input.Replace(match.Groups[0].ToString(), "0"); }
                        });
                    }

                    if (input.Contains("int"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"int\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), ((int)Evaluate(Replace(match.Groups[1].ToString().Trim(), false))).ToString());
                            }
                            catch { }
                        });
                    }

                    if (input.Contains("date"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"date\((.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), DateTime.Now.ToString(match.Groups[1].ToString().Trim()));
                            }
                            catch { }
                        });
                    }

                    if (input.Contains("sampleAudio"))
                    {
                        CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"sampleAudio\((.*?),(.*?)\)", includeEnd)), match =>
                        {
                            try
                            {
                                input = input.Replace(match.Groups[0].ToString(), Updater.GetSample(Parser.TryParse(match.Groups[1].ToString().Trim(), 0), Parser.TryParse(match.Groups[2].ToString().Trim(), 0f)).ToString());
                            }
                            catch { }
                        });
                    }

                    //if (input.Contains("copyEvent"))
                    //{
                    //    CoreHelper.RegexMatches(input, new Regex(GetFunctionPattern(@"copyEvent\((.*?)\)", includeEnd)), match =>
                    //    {
                    //        try
                    //        {
                    //            var type = (int)Evaluate(Replace(match.Groups[1].ToString(), false));
                    //            var valIndex = (int)Evaluate(Replace(match.Groups[2].ToString(), false));

                    //            input = input.Replace(match.Groups[0].ToString(), type.ToString());
                    //        }
                    //        catch { }
                    //    });
                    //}
                }
                catch (Exception ex)
                {
                    if (logException2)
                        CoreHelper.LogError($"Error!\nMath: {input}\nException: {ex}");
                }

                return input;
            }
            catch (Exception ex)
            {
                if (logException3)
                    CoreHelper.LogError($"Error!\nMath: {input}\nException: {ex}");
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
