using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

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
        public static bool SearchString(string searchTerm, params string[] items)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return true;

            for (int i = 0; i < items.Length; i++)
                if (items[i].ToLower().Contains(searchTerm.ToLower()))
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
        /// Formats a <see cref="DataManager.LevelRank"/> to have the proper style and color.
        /// </summary>
        /// <param name="levelRank">Level Rank to format.</param>
        /// <returns>Returns a formatted Level Rank.</returns>
        public static string FormatLevelRank(DataManager.LevelRank levelRank) => $"<#{LSColors.ColorToHex(levelRank.color)}><b>{levelRank.name}</b></color>";

        /// <summary>
        /// Custom text formatting.
        /// </summary>
        /// <param name="beatmapObject"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string FormatText(BeatmapObject beatmapObject, string str)
        {
            var currentAudioTime = AudioManager.inst.CurrentAudioSource.time;
            var currentAudioLength = AudioManager.inst.CurrentAudioSource.clip.length;

            if (str.Contains("math"))
            {
                RTString.RegexMatches(str, new Regex(@"<math=""(.*?)>"""), match =>
                {
                    try
                    {
                        str = str.Replace(match.Groups[0].ToString(), RTMath.Parse(match.Groups[1].ToString(), beatmapObject.GetObjectVariables()).ToString());
                    }
                    catch
                    {
                    }
                });

                RTString.RegexMatches(str, new Regex(@"<math=(.*?)>"), match =>
                {
                    try
                    {
                        str = str.Replace(match.Groups[0].ToString(), RTMath.Parse(match.Groups[1].ToString(), beatmapObject.GetObjectVariables()).ToString());
                    }
                    catch
                    {
                    }
                });
            }

            #region Audio

            #region Time Span

            if (str.Contains("msAudioSpan"))
                RTString.RegexMatches(str, new Regex(@"<msAudioSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToMilliSeconds(currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            if (str.Contains("sAudioSpan"))
                RTString.RegexMatches(str, new Regex(@"<sAudioSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToSeconds(currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            if (str.Contains("mAudioSpan"))
                RTString.RegexMatches(str, new Regex(@"<mAudioSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToMinutes(currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            if (str.Contains("hAudioSpan"))
                RTString.RegexMatches(str, new Regex(@"<hAudioSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToHours(currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            #endregion

            #region No Time Span

            if (str.Contains("msAudio"))
                RTString.RegexMatches(str, new Regex(@"<msAudio=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioTime) * 1000)));
                });

            if (str.Contains("sAudio"))
                RTString.RegexMatches(str, new Regex(@"<sAudio=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", currentAudioTime));
                });

            if (str.Contains("mAudio"))
                RTString.RegexMatches(str, new Regex(@"<mAudio=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioTime) / 60)));
                });

            if (str.Contains("hAudio"))
                RTString.RegexMatches(str, new Regex(@"<hAudio=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (currentAudioTime) / 600));
                });

            #endregion

            #endregion

            #region Audio Left

            #region Time Span

            if (str.Contains("msAudioLeftSpan"))
                RTString.RegexMatches(str, new Regex(@"<msAudioLeftSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToMilliSeconds(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            if (str.Contains("sAudioLeftSpan"))
                RTString.RegexMatches(str, new Regex(@"<sAudioLeftSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToSeconds(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            if (str.Contains("mAudioLeftSpan"))
                RTString.RegexMatches(str, new Regex(@"<mAudioLeftSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToMinutes(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            if (str.Contains("hAudioLeftSpan"))
                RTString.RegexMatches(str, new Regex(@"<hAudioLeftSpan=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), PreciseToHours(currentAudioLength - currentAudioTime, "{" + match.Groups[1].ToString() + "}"));
                });

            #endregion

            #region No Time Span

            if (str.Contains("msAudioLeft"))
                RTString.RegexMatches(str, new Regex(@"<msAudioLeft=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioLength - currentAudioTime) * 1000)));
                });

            if (str.Contains("sAudioLeft"))
                RTString.RegexMatches(str, new Regex(@"<sAudioLeft=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", currentAudioLength - currentAudioTime));
                });

            if (str.Contains("mAudioLeft"))
                RTString.RegexMatches(str, new Regex(@"<mAudioLeft=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (int)((currentAudioLength - currentAudioTime) / 60)));
                });

            if (str.Contains("hAudioLeft"))
                RTString.RegexMatches(str, new Regex(@"<hAudioLeft=([0-9.:]+)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), string.Format("{" + match.Groups[1].ToString() + "}", (currentAudioLength - currentAudioTime) / 600));
                });

            #endregion

            #endregion

            #region Real Time

            if (str.Contains("realTime"))
                RTString.RegexMatches(str, new Regex(@"<realTime=([a-z]+)>"), match =>
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
                RTString.RegexMatches(str, new Regex(@"<playerHealth=([0-9]+)>"), match =>
                {
                    if (int.TryParse(match.Groups[1].ToString(), out int index) && index < InputDataManager.inst.players.Count)
                        str = str.Replace(match.Groups[0].ToString(), InputDataManager.inst.players[index].health.ToString());
                    else
                        str = str.Replace(match.Groups[0].ToString(), "");
                });

            if (str.Contains("playerHealthBar"))
                RTString.RegexMatches(str, new Regex(@"<playerHealthBar=([0-9]+)>"), match =>
                {
                    if (int.TryParse(match.Groups[1].ToString(), out int index) && index < InputDataManager.inst.players.Count)
                    {
                        var player = PlayerManager.Players[index];
                        str = str.Replace(match.Groups[0].ToString(), ConvertHealthToEquals(player.Health, player.PlayerModel.basePart.health));
                    }
                    else
                        str = str.Replace(match.Groups[0].ToString(), "");
                });

            if (str.Contains("<playerHealthTotal>"))
            {
                var ph = 0;

                for (int j = 0; j < InputDataManager.inst.players.Count; j++)
                    ph += InputDataManager.inst.players[j].health;

                str = str.Replace("<playerHealthTotal>", ph.ToString());
            }

            if (str.Contains("<deathCount>"))
                str = str.Replace("<deathCount>", GameManager.inst.deaths.Count.ToString());

            if (str.Contains("<hitCount>"))
            {
                var pd = GameManager.inst.hits.Count;

                str = str.Replace("<hitCount>", pd.ToString());
            }

            if (str.Contains("<boostCount>"))
                str = str.Replace("<boostCount>", LevelManager.BoostCount.ToString());

            #endregion

            #region QuickElement

            if (str.Contains("quickElement"))
                RTString.RegexMatches(str, new Regex(@"<quickElement=(.*?)>"), match =>
                {
                    str = str.Replace(match.Groups[0].ToString(), QuickElementManager.ConvertQuickElement(beatmapObject, match.Groups[1].ToString()));
                });

            #endregion

            #region Random

            if (str.Contains("randomText"))
                RTString.RegexMatches(str, new Regex(@"<randomText=([0-9]+)>"), match =>
                {
                    if (int.TryParse(match.Groups[1].ToString(), out int length))
                        str = str.Replace(match.Groups[0].ToString(), LSText.randomString(length));
                });

            if (str.Contains("randomNumber"))
                RTString.RegexMatches(str, new Regex(@"<randomNumber=([0-9]+)>"), match =>
                {
                    if (int.TryParse(match.Groups[1].ToString(), out int length))
                        str = str.Replace(match.Groups[0].ToString(), LSText.randomNumString(length));
                });

            #endregion

            #region Theme

            var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

            if (str.Contains("themeObject"))
                RTString.RegexMatches(str, new Regex(@"<themeObject=([0-9]+)>"), match =>
                {
                    if (match.Groups.Count > 1)
                        str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetObjColor(int.Parse(match.Groups[1].ToString())))}>");
                });

            if (str.Contains("themeBGs"))
                RTString.RegexMatches(str, new Regex(@"<themeBGs=([0-9]+)>"), match =>
                {
                    if (match.Groups.Count > 1)
                        str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetBGColor(int.Parse(match.Groups[1].ToString())))}>");
                });

            if (str.Contains("themeFX"))
                RTString.RegexMatches(str, new Regex(@"<themeFX=([0-9]+)>"), match =>
                {
                    if (match.Groups.Count > 1)
                        str = str.Replace(match.Groups[0].Value, $"<#{LSColors.ColorToHex(beatmapTheme.GetFXColor(int.Parse(match.Groups[1].ToString())))}>");
                });

            if (str.Contains("themePlayers"))
                RTString.RegexMatches(str, new Regex(@"<themePlayers=([0-9]+)>"), match =>
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
                var levelRank =
                    !CoreHelper.InEditor && LevelManager.CurrentLevel != null ?
                        LevelManager.GetLevelRank(LevelManager.CurrentLevel) : CoreHelper.InEditor ? LevelManager.EditorRank :
                        DataManager.inst.levelRanks[0];

                str = str.Replace("<levelRank>", RTString.FormatLevelRank(levelRank));
            }

            if (str.Contains("<levelRankName>"))
            {
                var levelRank =
                    !CoreHelper.InEditor && LevelManager.CurrentLevel != null ?
                        LevelManager.GetLevelRank(LevelManager.CurrentLevel) :
                        DataManager.inst.levelRanks[0];

                str = str.Replace("<levelRankName>", levelRank.name);
            }

            if (str.Contains("<levelRankColor>"))
            {
                var levelRank =
                    !CoreHelper.InEditor && LevelManager.CurrentLevel != null ?
                        LevelManager.GetLevelRank(LevelManager.CurrentLevel) : CoreHelper.InEditor ? LevelManager.EditorRank :
                        DataManager.inst.levelRanks[0];

                str = str.Replace("<levelRankColor>", $"<color=#{LSColors.ColorToHex(levelRank.color)}>");
            }

            if (str.Contains("<levelRankCurrent>"))
            {
                var levelRank = !CoreHelper.InEditor ? LevelManager.GetLevelRank(GameManager.inst.hits) : LevelManager.EditorRank;

                str = str.Replace("<levelRankCurrent>", RTString.FormatLevelRank(levelRank));
            }

            if (str.Contains("<levelRankCurrentName>"))
            {
                var levelRank = !CoreHelper.InEditor ? LevelManager.GetLevelRank(GameManager.inst.hits) : LevelManager.EditorRank;

                str = str.Replace("<levelRankCurrentName>", levelRank.name);
            }

            if (str.Contains("<levelRankCurrentColor>"))
            {
                var levelRank = !CoreHelper.InEditor ? LevelManager.GetLevelRank(GameManager.inst.hits) : LevelManager.EditorRank;

                str = str.Replace("<levelRankCurrentColor>", $"<color=#{LSColors.ColorToHex(levelRank.color)}>");
            }

            // Level Rank Other
            {
                if (str.Contains("levelRankOther"))
                    RegexMatches(str, new Regex(@"<levelRankOther=([0-9]+)>"), match =>
                    {
                        DataManager.LevelRank levelRank;
                        if (LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level))
                            levelRank = LevelManager.GetLevelRank(level);
                        else
                            levelRank = CoreHelper.InEditor ? LevelManager.EditorRank : DataManager.inst.levelRanks[0];

                        str = str.Replace(match.Groups[0].ToString(), RTString.FormatLevelRank(levelRank));
                    });

                if (str.Contains("levelRankOtherName"))
                    RegexMatches(str, new Regex(@"<levelRankOtherName=([0-9]+)>"), match =>
                    {
                        DataManager.LevelRank levelRank;
                        if (LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level))
                            levelRank = LevelManager.GetLevelRank(level);
                        else
                            levelRank = CoreHelper.InEditor ? LevelManager.EditorRank : DataManager.inst.levelRanks[0];

                        str = str.Replace(match.Groups[0].ToString(), levelRank.name);
                    });

                if (str.Contains("levelRankOtherColor"))
                    RegexMatches(str, new Regex(@"<levelRankOtherColor=([0-9]+)>"), match =>
                    {
                        DataManager.LevelRank levelRank;
                        if (LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level))
                            levelRank = LevelManager.GetLevelRank(level);
                        else
                            levelRank = CoreHelper.InEditor ? LevelManager.EditorRank : DataManager.inst.levelRanks[0];

                        str = str.Replace(match.Groups[0].ToString(), $"<color=#{LSColors.ColorToHex(levelRank.color)}>");
                    });
            }

            if (str.Contains("<accuracy>"))
                str = str.Replace("<accuracy>", $"{LevelManager.CalculateAccuracy(GameManager.inst.hits.Count, AudioManager.inst.CurrentAudioSource.clip.length)}");

            #endregion

            #region Mod stuff

            if (str.Contains("modifierVariable"))
            {
                RTString.RegexMatches(str, new Regex(@"<modifierVariable=(.*?)>"), match =>
                {
                    var beatmapObject = CoreHelper.FindObjectWithTag(match.Groups[1].ToString());
                    if (beatmapObject)
                        str = str.Replace(match.Groups[0].ToString(), beatmapObject.integerVariable.ToString());
                });

                RTString.RegexMatches(str, new Regex(@"<modifierVariableID=(.*?)>"), match =>
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

        public static string ReplaceProperties(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            str = RTFile.ParsePaths(str);

            return str
                .Replace("{{GameVersion}}", ProjectArrhythmia.GameVersion.ToString())
                .Replace("{{ModVersion}}", LegacyPlugin.ModVersion.ToString())
                .Replace("{{DisplayName}}", CoreConfig.Instance.DisplayName.Value)
                .Replace("{{SplashText}}", LegacyPlugin.SplashText);
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
                    lowerIndex++;
                    input = input.Remove(i, 1);
                    input = input.Insert(i, lowerIndex.ToString() + seperator);
                }
                else if (AlphabetUpperIndex.TryGetValue(character, out int upperIndex))
                {
                    upperIndex++;
                    input = input.Remove(i, 1);
                    input = input.Insert(i, upperIndex.ToString() + seperator);
                }
                length = input.Length;
            }
            return input;
        }

        public static string CaeserEncrypt(string input, int count)
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
                    array[i] = alphabetLower[-lowerIndex];
                else if (AlphabetUpperIndex.TryGetValue(character, out int upperIndex))
                    array[i] = alphabetUpper[-upperIndex];
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

        public static char[] alphabetLower = new char[]
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        };
        public static char[] alphabetUpper = new char[]
        {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        };

        #endregion
    }
}
