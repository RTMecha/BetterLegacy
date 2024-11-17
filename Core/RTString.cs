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
        public static string FormatLevelRank(DataManager.LevelRank levelRank) => $"<color=#{LSColors.ColorToHex(levelRank.color)}><b>{levelRank.name}</b></color>";

    }
}
