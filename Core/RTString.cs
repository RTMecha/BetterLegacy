using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Menus;
using BetterLegacy.Story;

namespace BetterLegacy.Core
{
    /// <summary>
    /// String helper class.
    /// </summary>
    public static class RTString
    {
        public static string ReplaceFormatting(string str)
        {
            // Here we replace every instance of <formatting> in the text. Examples include <b>, <i>, <color=#FFFFFF>.
            var matches = Regex.Matches(str, "<(.*?)>");
            foreach (var obj in matches)
            {
                var match = (Match)obj;
                str = str.Replace(match.Groups[0].ToString(), "");
            }
            return str;
        }

        public static string ReplaceInsert(string input, string insert, int startIndex, int endIndex)
        {
            startIndex = Mathf.Clamp(startIndex, 0, input.Length - 1);
            endIndex = Mathf.Clamp(endIndex, 0, input.Length - 1);

            while (startIndex <= endIndex)
            {
                var list = input.ToCharArray().ToList();
                list.RemoveAt(startIndex);

                input = new string(list.ToArray());
                endIndex--;
            }

            return input.Insert(startIndex, insert);
        }

        public static string SectionString(string input, int startIndex, int endIndex) => input.Substring(startIndex, endIndex - startIndex + 1);

        /// <summary>
        /// Splits the string based on a set of strings.
        /// </summary>
        /// <param name="input">String input.</param>
        /// <param name="array">Array to split.</param>
        /// <returns>Returns a string array split by the array.</returns>
        public static string[] GetLines(string input, params string[] array) => input.Split(array, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// Splits the lines of the input string into an array.
        /// </summary>
        /// <param name="input">String input.</param>
        /// <returns>Returns a string array representing the lines of the input string.</returns>
        public static string[] GetLines(string input) => input.Split(new string[] { "\n", "\r\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// Interpolates through a string, typewriter style.
        /// </summary>
        /// <param name="input">String input.</param>
        /// <param name="t">Time scale.</param>
        /// <returns>Returns an interpolated string.</returns>
        public static string InterpolateString(string input, float t) => input.Substring(0, Mathf.Clamp((int)RTMath.Lerp(0, input.Length, t), 0, input.Length));

        /// <summary>
        /// Tries to find a match in the input string and outputs the match.
        /// </summary>
        /// <param name="input">String input.</param>
        /// <param name="regex">Regex to find.</param>
        /// <param name="match">Match output.</param>
        /// <returns>Returns true if a match was found, otherwise false.</returns>
        public static bool RegexMatch(string input, Regex regex, out Match match)
        {
            match = regex?.Match(input);
            return match != null && match.Success;
        }

        /// <summary>
        /// Invokes an action if a match was found in the input string.
        /// </summary>
        /// <param name="input">String input.</param>
        /// <param name="regex">Regex to find.</param>
        /// <param name="matchAction">Function to run for the match.</param>
        public static void RegexMatch(string input, Regex regex, Action<Match> matchAction)
        {
            if (RegexMatch(input, regex, out Match match))
                matchAction?.Invoke(match);
        }

        /// <summary>
        /// Invokes an action for each Regex match in an input string.
        /// </summary>
        /// <param name="input">String input.</param>
        /// <param name="regex">Regex to find.</param>
        /// <param name="matchAction">Function to run for each match.</param>
        public static void RegexMatches(string input, Regex regex, Action<Match> matchAction)
        {
            var matchCollection = regex.Matches(input);
            foreach (Match match in matchCollection)
                matchAction?.Invoke(match);
        }

        /// <summary>
        /// Flips all references to "left" and "right" in a string.
        /// </summary>
        /// <param name="input">String input.</param>
        /// <returns>Returns a string with flipped left / right.</returns>
        public static string FlipLeftRight(string input)
        {
            input = input.Replace("Left", "LSLeft87344874")
                .Replace("Right", "LSRight87344874")
                .Replace("left", "LSleft87344874")
                .Replace("right", "LSright87344874")
                .Replace("LEFT", "LSLEFT87344874")
                .Replace("RIGHT", "LSRIGHT87344874");

            return input.Replace("LSLeft87344874", "Right")
                .Replace("LSRight87344874", "Left")
                .Replace("LSleft87344874", "right")
                .Replace("LSright87344874", "left")
                .Replace("LSLEFT87344874", "RIGHT")
                .Replace("LSRIGHT87344874", "LEFT");
        }

        /// <summary>
        /// Flips all references to "up" and "down" in a string.
        /// </summary>
        /// <param name="input">String input.</param>
        /// <returns>Returns a string with flipped up / down.</returns>
        public static string FlipUpDown(string input)
        {
            input = input.Replace("Up", "LSUp87344874")
                .Replace("Down", "LSDown87344874")
                .Replace("up", "LSup87344874")
                .Replace("down", "LSdown87344874")
                .Replace("UP", "LSUP87344874")
                .Replace("DOWN", "LSDOWN87344874");

            return input.Replace("LSUp87344874", "Down")
                .Replace("LSDown87344874", "Up")
                .Replace("LSup87344874", "down")
                .Replace("LSdown87344874", "up")
                .Replace("LSUP87344874", "DOWN")
                .Replace("LSDOWN87344874", "UP");
        }

        /// <summary>
        /// Flips all references to "upper" and "lower" in a string.
        /// </summary>
        /// <param name="input">String input.</param>
        /// <returns>Returns a string with flipped upper / lower.</returns>
        public static string FlipUpperLower(string input)
        {
            input = input.Replace("Upper", "LSUpper87344874")
                .Replace("Lower", "LSLower87344874")
                .Replace("upper", "LSupper87344874")
                .Replace("lower", "LSlower87344874")
                .Replace("UPPER", "LSUPPER87344874")
                .Replace("LOWER", "LSLOWER87344874");

            return input.Replace("LSUpper87344874", "Lower")
                .Replace("LSLower87344874", "Upper")
                .Replace("LSupper87344874", "lower")
                .Replace("LSlower87344874", "upper")
                .Replace("LSUPPER87344874", "LOWER")
                .Replace("LSLOWER87344874", "UPPER");
        }

        /// <summary>
        /// Method used for search fields. Checks if the search term is empty or the item contains the search term.
        /// </summary>
        /// <param name="searchTerm">Search field input.</param>
        /// <param name="item">Item to compare.</param>
        /// <returns>Return true if search is found, otherwise returns false.</returns>
        public static bool SearchString(string searchTerm, string item) => string.IsNullOrEmpty(searchTerm) || item.ToLower().Contains(searchTerm.ToLower());

        /// <summary>
        /// Method used for search fields. Checks if the search term is empty or any of the items contain the search term.
        /// </summary>
        /// <param name="searchTerm">Search field input.</param>
        /// <param name="items">Items to compare.</param>
        /// <returns>Return true if search is found, otherwise returns false.</returns>
        public static bool SearchString(string searchTerm, params SearchMatcherBase[] items)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return true;

            var term = searchTerm.ToLower();
            for (int i = 0; i < items.Length; i++)
                if (items[i].Match(term))
                    return true;

            return false;
        }

        /// <summary>
        /// Converts a list to a string, for example: 0, 1, 2, 3.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="list">List to get a string from.</param>
        /// <returns>Returns a string representing the list.</returns>
        public static string ListToString<T>(List<T> list)
        {
            string s = "";
            if (list.Count > 0)
                for (int i = 0; i < list.Count; i++)
                {
                    s += list[i].ToString();
                    if (i != list.Count - 1)
                        s += ", ";
                }
            return s;
        }

        /// <summary>
        /// Converts an array to a string, for example: 0, 1, 2, 3.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="array">Array to get a string from.</param>
        /// <returns>Returns a string representing the array.</returns>
        public static string ArrayToString<T>(T[] array)
        {
            string s = "";
            if (array.Length > 0)
                for (int i = 0; i < array.Length; i++)
                {
                    s += array[i].ToString();
                    if (i != array.Length - 1)
                        s += ", ";
                }
            return s;
        }

        /// <summary>
        /// Converts an array to a string, for example: 0, 1, 2, 3.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="List{T}"/>.</typeparam>
        /// <param name="p">Array to get a string from.</param>
        /// <returns>Returns a string representing the array.</returns>
        public static string ArrayToString(params object[] p)
        {
            string s = "";
            if (p.Length > 0)
                for (int i = 0; i < p.Length; i++)
                {
                    s += p[i].ToString();
                    if (i != p.Length - 1)
                        s += ", ";
                }
            return s;
        }

        /// <summary>
        /// Custom text formatting.
        /// </summary>
        /// <param name="beatmapObject"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FormatText(BeatmapObject beatmapObject, string str, Dictionary<string, string> variables = null)
        {
            var currentAudioTime = AudioManager.inst.CurrentAudioSource.time;
            var currentAudioLength = AudioManager.inst.CurrentAudioSource.clip.length;

            if (variables != null)
                foreach (var variable in variables)
                    str = str.Replace($"<var={variable.Key}>", variable.Value);

            if (str.Contains("math"))
            {
                RegexMatches(str, new Regex(@"<math=""(.*?)>"""), match =>
                {
                    try
                    {
                        var numberVariables = beatmapObject.GetObjectVariables();
                        ModifiersHelper.SetVariables(variables, numberVariables);

                        str = str.Replace(match.Groups[0].ToString(), RTMath.Parse(match.Groups[1].ToString(), numberVariables, beatmapObject.GetObjectFunctions()).ToString());
                    }
                    catch
                    {
                    }
                });

                RegexMatches(str, new Regex(@"<math=(.*?)>"), match =>
                {
                    try
                    {
                        var numberVariables = beatmapObject.GetObjectVariables();
                        ModifiersHelper.SetVariables(variables, numberVariables);

                        str = str.Replace(match.Groups[0].ToString(), RTMath.Parse(match.Groups[1].ToString(), numberVariables, beatmapObject.GetObjectFunctions()).ToString());
                    }
                    catch
                    {
                    }
                });
            }

            #region Audio

            #region Time Span

            if (str.Contains("msAudioSpan"))
                RegexMatches(str, new Regex(@"<msAudioSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToMilliSeconds(currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            if (str.Contains("sAudioSpan"))
                RegexMatches(str, new Regex(@"<sAudioSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToSeconds(currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            if (str.Contains("mAudioSpan"))
                RegexMatches(str, new Regex(@"<mAudioSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToMinutes(currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            if (str.Contains("hAudioSpan"))
                RegexMatches(str, new Regex(@"<hAudioSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToHours(currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            #endregion

            #region No Time Span

            if (str.Contains("msAudio"))
                RegexMatches(str, new Regex(@"<msAudio=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioTime) * 1000)));
                });

            if (str.Contains("sAudio"))
                RegexMatches(str, new Regex(@"<sAudio=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", currentAudioTime));
                });

            if (str.Contains("mAudio"))
                RegexMatches(str, new Regex(@"<mAudio=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioTime) / 60)));
                });

            if (str.Contains("hAudio"))
                RegexMatches(str, new Regex(@"<hAudio=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (currentAudioTime) / 600));
                });

            #endregion

            #endregion

            #region Audio Left

            #region Time Span

            if (str.Contains("msAudioLeftSpan"))
                RegexMatches(str, new Regex(@"<msAudioLeftSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToMilliSeconds(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            if (str.Contains("sAudioLeftSpan"))
                RegexMatches(str, new Regex(@"<sAudioLeftSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToSeconds(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            if (str.Contains("mAudioLeftSpan"))
                RegexMatches(str, new Regex(@"<mAudioLeftSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToMinutes(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            if (str.Contains("hAudioLeftSpan"))
                RegexMatches(str, new Regex(@"<hAudioLeftSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToHours(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            #endregion

            #region No Time Span

            if (str.Contains("msAudioLeft"))
                RegexMatches(str, new Regex(@"<msAudioLeft=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioLength - currentAudioTime) * 1000)));
                });

            if (str.Contains("sAudioLeft"))
                RegexMatches(str, new Regex(@"<sAudioLeft=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", currentAudioLength - currentAudioTime));
                });

            if (str.Contains("mAudioLeft"))
                RegexMatches(str, new Regex(@"<mAudioLeft=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioLength - currentAudioTime) / 60)));
                });

            if (str.Contains("hAudioLeft"))
                RegexMatches(str, new Regex(@"<hAudioLeft=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (currentAudioLength - currentAudioTime) / 600));
                });

            #endregion

            #endregion

            #region Real Time

            if (str.Contains("realTime"))
                RegexMatches(str, new Regex(@"<realTime=([a-z]+)>"), match =>
                {
                    try
                    {
                        str = str.Replace(match.Groups[0].ToString(), DateTime.Now.ToString(match.Groups[1].ToString()));
                    }
                    catch
                    {
                    }
                });

            #endregion

            #region Players

            if (str.Contains("playerHealth"))
                RegexMatches(str, new Regex(@"<playerHealth=([0-9]+)>"), match =>
                {
                    if (int.TryParse(match.Groups[1].ToString(), out int index) && index < PlayerManager.Players.Count)
                        str = str.Replace(match.Groups[0].ToString(), PlayerManager.Players[index].health.ToString());
                    else
                        str = str.Replace(match.Groups[0].ToString(), "");
                });

            if (str.Contains("playerHealthBar"))
                RegexMatches(str, new Regex(@"<playerHealthBar=([0-9]+)>"), match =>
                {
                    if (int.TryParse(match.Groups[1].ToString(), out int index) && index < PlayerManager.Players.Count)
                    {
                        var player = PlayerManager.Players[index];
                        str = str.Replace(match.Groups[0].ToString(), ConvertHealthToEquals(player.Health, player.GetControl()?.Health ?? 3));
                    }
                    else
                        str = str.Replace(match.Groups[0].ToString(), "");
                });

            if (str.Contains("<playerHealthTotal>"))
            {
                var ph = 0;

                for (int j = 0; j < PlayerManager.Players.Count; j++)
                    ph += PlayerManager.Players[j].health;

                str = str.Replace("<playerHealthTotal>", ph.ToString());
            }

            if (str.Contains("<deathCount>"))
                str = str.Replace("<deathCount>", RTBeatmap.Current.deaths.Count.ToString());

            if (str.Contains("<hitCount>"))
            {
                var pd = RTBeatmap.Current.hits.Count;

                str = str.Replace("<hitCount>", pd.ToString());
            }

            if (str.Contains("<boostCount>"))
                str = str.Replace("<boostCount>", RTBeatmap.Current.boosts.Count.ToString());

            #endregion

            #region QuickElement

            if (str.Contains("quickElement"))
                RegexMatches(str, new Regex(@"<quickElement=(.*?)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), QuickElementManager.ConvertQuickElement(beatmapObject, match.Groups[1].ToString()));
                });

            #endregion

            #region Random

            if (str.Contains("randomText"))
                RegexMatches(str, new Regex(@"<randomText=([0-9]+)>"), match =>
                {
                    if (int.TryParse(match.Groups[1].ToString(), out int length))
                        str = str.Replace(match.Groups[0].ToString(), LSText.randomString(length));
                });

            if (str.Contains("randomNumber"))
                RegexMatches(str, new Regex(@"<randomNumber=([0-9]+)>"), match =>
                {
                    if (int.TryParse(match.Groups[1].ToString(), out int length))
                        str = str.Replace(match.Groups[0].ToString(), LSText.randomNumString(length));
                });

            #endregion

            #region Theme

            var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

            if (str.Contains("themeObject"))
                RegexMatches(str, new Regex(@"<themeObject=([0-9]+)>"), match =>
                {
                    if (match.Groups.Count > 1)
                        str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetObjColor(int.Parse(match.Groups[1].ToString())))}>");
                });

            if (str.Contains("themeBGs"))
                RegexMatches(str, new Regex(@"<themeBGs=([0-9]+)>"), match =>
                {
                    if (match.Groups.Count > 1)
                        str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetBGColor(int.Parse(match.Groups[1].ToString())))}>");
                });

            if (str.Contains("themeFX"))
                RegexMatches(str, new Regex(@"<themeFX=([0-9]+)>"), match =>
                {
                    if (match.Groups.Count > 1)
                        str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetFXColor(int.Parse(match.Groups[1].ToString())))}>");
                });

            if (str.Contains("themePlayers"))
                RegexMatches(str, new Regex(@"<themePlayers=([0-9]+)>"), match =>
                {
                    if (match.Groups.Count > 1)
                        str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetPlayerColor(int.Parse(match.Groups[1].ToString())))}>");
                });

            if (str.Contains("<themeBG>"))
                str = str.Replace("<themeBG>", LSColors.ColorToHex(beatmapTheme.backgroundColor));

            if (str.Contains("<themeGUI>"))
                str = str.Replace("<themeGUI>", LSColors.ColorToHex(beatmapTheme.guiColor));

            if (str.Contains("<themeTail>"))
                str = str.Replace("<themeTail>", LSColors.ColorToHex(beatmapTheme.guiAccentColor));

            #endregion

            #region LevelRank

            if (str.Contains("<levelRank>"))
            {
                var rank =
                    !CoreHelper.InEditor && LevelManager.CurrentLevel != null ?
                        LevelManager.GetLevelRank(LevelManager.CurrentLevel) : CoreHelper.InEditor ? Rank.EditorRank :
                        Rank.Null;

                str = str.Replace("<levelRank>", rank.Format());
            }

            if (str.Contains("<levelRankName>"))
            {
                var rank =
                    !CoreHelper.InEditor && LevelManager.CurrentLevel != null ?
                        LevelManager.GetLevelRank(LevelManager.CurrentLevel) :
                        Rank.Null;

                str = str.Replace("<levelRankName>", rank.DisplayName);
            }

            if (str.Contains("<levelRankColor>"))
            {
                var rank =
                    !CoreHelper.InEditor && LevelManager.CurrentLevel != null ?
                        LevelManager.GetLevelRank(LevelManager.CurrentLevel) : CoreHelper.InEditor ? Rank.EditorRank :
                        Rank.Null;

                str = str.Replace("<levelRankColor>", $"<color=#{LSColors.ColorToHex(rank.Color)}>");
            }

            if (str.Contains("<levelRankCurrent>"))
            {
                var rank = !CoreHelper.InEditor ? LevelManager.GetLevelRank(RTBeatmap.Current.hits) : Rank.EditorRank;

                str = str.Replace("<levelRankCurrent>", rank.Format());
            }

            if (str.Contains("<levelRankCurrentName>"))
            {
                var rank = !CoreHelper.InEditor ? LevelManager.GetLevelRank(RTBeatmap.Current.hits) : Rank.EditorRank;

                str = str.Replace("<levelRankCurrentName>", rank.DisplayName);
            }

            if (str.Contains("<levelRankCurrentColor>"))
            {
                var rank = !CoreHelper.InEditor ? LevelManager.GetLevelRank(RTBeatmap.Current.hits) : Rank.EditorRank;

                str = str.Replace("<levelRankCurrentColor>", $"<color=#{LSColors.ColorToHex(rank.Color)}>");
            }

            // Level Rank Other
            {
                if (str.Contains("levelRankOther"))
                    RegexMatches(str, new Regex(@"<levelRankOther=([0-9]+)>"), match =>
                    {
                        Rank rank;
                        if (LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level))
                            rank = LevelManager.GetLevelRank(level);
                        else
                            rank = CoreHelper.InEditor ? Rank.EditorRank : Rank.Null;

                        str = str.Replace(match.Groups[0].ToString(), rank.Format());
                    });

                if (str.Contains("levelRankOtherName"))
                    RegexMatches(str, new Regex(@"<levelRankOtherName=([0-9]+)>"), match =>
                    {
                        Rank rank;
                        if (LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level))
                            rank = LevelManager.GetLevelRank(level);
                        else
                            rank = CoreHelper.InEditor ? Rank.EditorRank : Rank.Null;

                        str = str.Replace(match.Groups[0].ToString(), rank.DisplayName);
                    });

                if (str.Contains("levelRankOtherColor"))
                    RegexMatches(str, new Regex(@"<levelRankOtherColor=([0-9]+)>"), match =>
                    {
                        Rank rank;
                        if (LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level))
                            rank = LevelManager.GetLevelRank(level);
                        else
                            rank = CoreHelper.InEditor ? Rank.EditorRank : Rank.Null;

                        str = str.Replace(match.Groups[0].ToString(), $"<color=#{LSColors.ColorToHex(rank.Color)}>");
                    });
            }

            if (str.Contains("<accuracy>"))
                str = str.Replace("<accuracy>", $"{LevelManager.CalculateAccuracy(RTBeatmap.Current.hits.Count, AudioManager.inst.CurrentAudioSource.clip.length)}");

            #endregion

            #region Mod stuff

            if (str.Contains("modifierVariable"))
            {
                RegexMatches(str, new Regex(@"<modifierVariable=(.*?)>"), match =>
                {
                    var beatmapObject = GameData.Current.FindObjectWithTag(match.Groups[1].ToString());
                    if (beatmapObject)
                        str = str.Replace(match.Groups[0].ToString(), beatmapObject.integerVariable.ToString());
                });

                RegexMatches(str, new Regex(@"<modifierVariableID=(.*?)>"), match =>
                {
                    var beatmapObject = GameData.Current.beatmapObjects.Find(x => x.id == match.Groups[1].ToString());
                    if (beatmapObject)
                        str = str.Replace(match.Groups[0].ToString(), beatmapObject.integerVariable.ToString());
                });
            }

            if (str.Contains("<username>"))
                str = str.Replace("<username>", CoreConfig.Instance.DisplayName.Value);

            if (str.Contains("<modVersion>"))
                str = str.Replace("<modVersion>", LegacyPlugin.ModVersion.ToString());

            #endregion

            return str;
        }

        public static string ParseText(string input, Dictionary<string, SimpleJSON.JSONNode> customVariables = null)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            RegexMatches(input, new Regex(@"{{LevelRank=([0-9]+)}}"), match =>
            {
                Rank rank =
                    LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level) ? LevelManager.GetLevelRank(level) :
                    CoreHelper.InEditor ?
                        Rank.EditorRank :
                        Rank.Null;

                input = input.Replace(match.Groups[0].ToString(), rank.Format());
            });

            RegexMatches(input, new Regex(@"{{StoryLevelRank=([0-9]+)}}"), match =>
            {
                Rank rank =
                    StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == match.Groups[1].ToString(), out SaveData playerData) ? LevelManager.GetLevelRank(playerData) :
                    CoreHelper.InEditor ?
                        Rank.EditorRank :
                        Rank.Null;

                input = input.Replace(match.Groups[0].ToString(), rank.Format());
            });

            RegexMatches(input, new Regex(@"{{RandomNumber=([0-9]+)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), LSText.randomNumString(Parser.TryParse(match.Groups[1].ToString(), 0)));
            });

            RegexMatches(input, new Regex(@"{{RandomText=([0-9]+)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), LSText.randomString(Parser.TryParse(match.Groups[1].ToString(), 0)));
            });
            
            RegexMatches(input, new Regex(@"{{ToStoryNumber=([0-9]+)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), ToStoryNumber(Parser.TryParse(match.Groups[1].ToString(), 0)));
            });

            RegexMatches(input, new Regex("{{ParseVariable=\"(.*?)\"}}"), match =>
            {
                var jn = SimpleJSON.JSON.Parse(match.Groups[1].ToString());
                input = input.Replace(match.Groups[0].ToString(), InterfaceManager.inst.ParseVarFunction(jn, customVariables: customVariables));
            });

            input = RTFile.ParsePaths(input);

            if (InterfaceManager.inst.CurrentInterface || CoreHelper.InStory)
            {
                RegexMatches(input, new Regex(@"{{LoadStoryString=(.*?),(.*?)}}"), match =>
                {
                    input = input.Replace(match.Groups[0].ToString(), StoryManager.inst.CurrentSave.LoadString(match.Groups[1].ToString(), match.Groups[2].ToString()));
                });

                input = input
                .Replace("{{CurrentPlayingChapterNumber}}", ToStoryNumber(StoryManager.inst.currentPlayingChapterIndex))
                .Replace("{{CurrentPlayingLevelNumber}}", ToStoryNumber(StoryManager.inst.currentPlayingLevelSequenceIndex))
                .Replace("{{SaveSlotNumber}}", ToStoryNumber(StoryManager.inst.CurrentSave.Slot))
                ;
            }

            return input
                .Replace("{{GameVersion}}", ProjectArrhythmia.GameVersion.ToString())
                .Replace("{{ModVersion}}", LegacyPlugin.ModVersion.ToString())
                .Replace("{{DisplayName}}", CoreConfig.Instance.DisplayName.Value)
                .Replace("{{SplashText}}", LegacyPlugin.SplashText)
                ;
        }

        public static string ToStoryNumber(int num) => (num + 1).ToString("00");

        public static string PreciseToMilliSeconds(float seconds, string format = "{0:000}") => string.Format(format, TimeSpan.FromSeconds(seconds).Milliseconds);

        public static string PreciseToSeconds(float seconds, string format = "{0:00}") => string.Format(format, TimeSpan.FromSeconds(seconds).Seconds);

        public static string PreciseToMinutes(float seconds, string format = "{0:00}") => string.Format(format, TimeSpan.FromSeconds(seconds).Minutes);

        public static string PreciseToHours(float seconds, string format = "{0:00}") => string.Format(format, TimeSpan.FromSeconds(seconds).Hours);

        public static string SecondsToTime(float seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            return seconds >= 86400f ? string.Format("{0:D0}:{1:D1}:{2:D2}:{3:D3}", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds) : string.Format("{0:D0}:{1:D1}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        }

        public static string Percentage(float t, float length) => string.Format("{0:000}", (int)RTMath.Percentage(t, length));

        public static string ConvertHealthToEquals(int health, int max = 3)
        {
            string result = "[";
            for (int i = 0; i < health; i++)
                result += "=";

            int spaces = -health + max;
            if (spaces > 0)
                for (int i = 0; i < spaces; i++)
                    result += " ";

            return result += "]";
        }

        public static string ConvertBar(string s, float progress, int count = 10)
        {
            var result = "";
            for (int i = 0; i < (int)(progress / count); i++)
                result += s;
            while (result.Length < count)
                result = " " + result;

            return result;
        }

        public static string ReplaceSpace(string input) => input?.Replace(" ", "_");

        /// <summary>
        /// Splits the words in a single word.
        /// </summary>
        /// <param name="input">String input.</param>
        /// <returns>Returns a split set of words based on capitals. For example: ParaBoss > Para Boss or paLegacy > pa Legacy.</returns>
        public static string SplitWords(string input)
        {
            int index = 0;
            while (index < input.Length)
            {
                if (char.IsUpper(input[index]))
                {
                    input = input.Insert(index, " ");
                    index++;
                }

                index++;
            }

            return input;
        }

        public static string GetClipboardText() => Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;

        #region Code Encryptions

        public static string ByteEncrypt(string input, string seperator = " ")
        {
            int length = input.Length;
            string result = "";
            for (int i = 0; i < length; i++)
                result += ((byte)input[i]).ToString() + seperator;
            return result;
        }

        public static string A1Z26Encrypt(string input, string seperator = " ")
        {
            int length = input.Length;
            for (int i = 0; i < length; i++)
            {
                var character = input[i];
                if (AlphabetLowerIndex.TryGetValue(character, out int lowerIndex))
                {
                    input = input.Remove(i, 1);
                    input = input.Insert(i, (lowerIndex + 1).ToString() + seperator);
                }
                else if (AlphabetUpperIndex.TryGetValue(character, out int upperIndex))
                {
                    input = input.Remove(i, 1);
                    input = input.Insert(i, (upperIndex + 1).ToString() + seperator);
                }
                length = input.Length;
            }
            return input;
        }

        public static string CaeserEncrypt(string input, int count = 3)
        {
            var array = input.ToCharArray();
            for (int i = 0; i < input.Length; i++)
            {
                var character = input[i];
                if (AlphabetLowerIndex.TryGetValue(character, out int lowerIndex))
                    array[i] = alphabetLower[(lowerIndex + count) % alphabetLower.Length];
                else if (AlphabetUpperIndex.TryGetValue(character, out int upperIndex))
                    array[i] = alphabetUpper[(upperIndex + count) % alphabetUpper.Length];
                else
                    array[i] = character;
            }
            return new string(array);
        }

        public static string AtbashEncrypt(string input)
        {
            var array = input.ToCharArray();
            for (int i = 0; i < input.Length; i++)
            {
                var character = input[i];
                if (AlphabetLowerIndex.TryGetValue(character, out int lowerIndex))
                    array[i] = alphabetLower[-lowerIndex + alphabetLower.Length - 1];
                else if (AlphabetUpperIndex.TryGetValue(character, out int upperIndex))
                    array[i] = alphabetUpper[-upperIndex + alphabetUpper.Length - 1];
                else
                    array[i] = character;
            }
            return new string(array);
        }

        public static Dictionary<char, int> AlphabetLowerIndex
        {
            get
            {
                var dictionary = new Dictionary<char, int>();
                for (int i = 0; i < alphabetLower.Length; i++)
                    dictionary[alphabetLower[i]] = i;
                return dictionary;
            }
        }
        
        public static Dictionary<char, int> AlphabetUpperIndex
        {
            get
            {
                var dictionary = new Dictionary<char, int>();
                for (int i = 0; i < alphabetUpper.Length; i++)
                    dictionary[alphabetUpper[i]] = i;
                return dictionary;
            }
        }

        public static char[] alphabetLower = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', };
        public static char[] alphabetUpper = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', };

        #endregion
    }

    /// <summary>
    /// Helper class for searching strings.
    /// </summary>
    public class SearchMatcher : SearchMatcherBase
    {
        public SearchMatcher(string value, SearchMatchType matchType = SearchMatchType.Contains)
        {
            this.value = value;
            MatchType = matchType;
        }

        /// <summary>
        /// Value of the object that is searched for.
        /// </summary>
        public string value;

        public override bool Match(string searchTerm) => MatchType switch
        {
            SearchMatchType.Exact => value == searchTerm,
            SearchMatchType.Contains => value.ToLower().Contains(searchTerm),
            _ => true,
        };
    }

    public class SearchArrayMatcher : SearchMatcherBase
    {
        public SearchArrayMatcher(string[] value, SearchMatchType matchType = SearchMatchType.Contains)
        {
            this.value = value;
            MatchType = matchType;
        }

        /// <summary>
        /// Value of the object that is searched for.
        /// </summary>
        public string[] value;

        public override bool Match(string searchTerm) => MatchType switch
        {
            SearchMatchType.Exact => value.Contains(searchTerm),
            SearchMatchType.Contains => value.Any(x => x.ToLower().Contains(searchTerm)),
            _ => true,
        };
    }

    public class SearchListMatcher : SearchMatcherBase
    {
        public SearchListMatcher(List<string> value, SearchMatchType matchType = SearchMatchType.Contains)
        {
            this.value = value;
            MatchType = matchType;
        }

        /// <summary>
        /// Value of the object that is searched for.
        /// </summary>
        public List<string> value;

        public override bool Match(string searchTerm) => MatchType switch
        {
            SearchMatchType.Exact => value.Contains(searchTerm),
            SearchMatchType.Contains => value.Any(x => x.ToLower().Contains(searchTerm)),
            _ => true,
        };
    }

    /// <summary>
    /// Indicates the object is a search helper.
    /// </summary>
    public abstract class SearchMatcherBase
    {
        /// <summary>
        /// Match type.
        /// </summary>
        public virtual SearchMatchType MatchType { get; set; }

        /// <summary>
        /// Checks if the search term matches <see cref="value"/>.
        /// </summary>
        /// <param name="searchTerm">Search term.</param>
        /// <returns>Returns true if a match is found, otherwise returns false.</returns>
        public abstract bool Match(string searchTerm);

        public static implicit operator SearchMatcherBase(int value) => new SearchMatcher(value.ToString(), SearchMatchType.Exact);

        public static implicit operator SearchMatcherBase(string value) => new SearchMatcher(value);

        public static implicit operator SearchMatcherBase(string[] value) => new SearchArrayMatcher(value);
    }

    /// <summary>
    /// Match behavior.
    /// </summary>
    public enum SearchMatchType
    {
        /// <summary>
        /// Search string and value should be the same.
        /// </summary>
        Exact,
        /// <summary>
        /// The value should contain the search string.
        /// </summary>
        Contains,
    }
}
