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

        public static string ReplaceInsert(string str, string insert, int startIndex, int endIndex)
        {
            startIndex = Mathf.Clamp(startIndex, 0, str.Length - 1);
            endIndex = Mathf.Clamp(endIndex, 0, str.Length - 1);

            //if (endIndex <= startIndex)
            //    return str;

            while (startIndex <= endIndex)
            {
                var list = str.ToCharArray().ToList();
                list.RemoveAt(startIndex);

                str = new string(list.ToArray());
                endIndex--;
            }

            return str.Insert(startIndex, insert);
        }

        public static string SectionString(string str, int startIndex, int endIndex) => str.Substring(startIndex, endIndex - startIndex + 1);

        public static string[] GetLines(string str) => str.Split(new string[] { "\n", "\r\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

        public static string InterpolateString(string str, float t) => str.Substring(0, Mathf.Clamp((int)RTMath.Lerp(0, str.Length, t), 0, str.Length));

        public static KeyValuePair<string, string> ReplaceMatching(KeyValuePair<string, string> keyValuePair, string sequenceText, string pattern)
        {
            var text = keyValuePair.Key;
            var replace = keyValuePair.Value;
            var matches1 = Regex.Matches(text, pattern);
            for (int i = 0; i < matches1.Count; i++)
            {
                var m = matches1[i];
                if (!sequenceText.Contains(m.Groups[0].ToString()))
                    text = text.Replace(m.Groups[0].ToString(), "");
                replace = replace.Replace(m.Groups[0].ToString(), "");
            }
            return new KeyValuePair<string, string>(text, replace);
        }

        public static bool RegexMatch(string str, Regex regex, out Match match)
        {
            if (regex != null)
            {
                match = regex.Match(str);
                return match.Success;
            }

            match = null;
            return false;
        }

        public static void RegexMatch(string str, Regex regex, Action<Match> matchAction)
        {
            if (RegexMatch(str, regex, out Match match))
                matchAction?.Invoke(match);
        }

        public static void RegexMatches(string str, Regex regex, Action<Match> matchAction)
        {
            var matchCollection = regex.Matches(str);
            foreach (Match match in matchCollection)
                matchAction?.Invoke(match);
        }

        public static string FlipLeftRight(string str)
        {
            string s;
            s = str.Replace("Left", "LSLeft87344874")
                .Replace("Right", "LSRight87344874")
                .Replace("left", "LSleft87344874")
                .Replace("right", "LSright87344874")
                .Replace("LEFT", "LSLEFT87344874")
                .Replace("RIGHT", "LSRIGHT87344874");

            return s.Replace("LSLeft87344874", "Right")
                .Replace("LSRight87344874", "Left")
                .Replace("LSleft87344874", "right")
                .Replace("LSright87344874", "left")
                .Replace("LSLEFT87344874", "RIGHT")
                .Replace("LSRIGHT87344874", "LEFT");
        }

        public static string FlipUpDown(string str)
        {
            string s;
            s = str.Replace("Up", "LSUp87344874")
                .Replace("Down", "LSDown87344874")
                .Replace("up", "LSup87344874")
                .Replace("down", "LSdown87344874")
                .Replace("UP", "LSUP87344874")
                .Replace("DOWN", "LSDOWN87344874");

            return s.Replace("LSUp87344874", "Down")
                .Replace("LSDown87344874", "Up")
                .Replace("LSup87344874", "down")
                .Replace("LSdown87344874", "up")
                .Replace("LSUP87344874", "DOWN")
                .Replace("LSDOWN87344874", "UP");
        }

        public static string FlipUpperLower(string str)
        {
            string s;
            s = str.Replace("Upper", "LSUpper87344874")
                .Replace("Lower", "LSLower87344874")
                .Replace("upper", "LSupper87344874")
                .Replace("lower", "LSlower87344874")
                .Replace("UPPER", "LSUPPER87344874")
                .Replace("LOWER", "LSLOWER87344874");

            return s.Replace("LSUpper87344874", "Lower")
                .Replace("LSLower87344874", "Upper")
                .Replace("LSupper87344874", "lower")
                .Replace("LSlower87344874", "upper")
                .Replace("LSUPPER87344874", "LOWER")
                .Replace("LSLOWER87344874", "UPPER");
        }

        public static bool ColorMatch(Color a, Color b, float range, bool alpha = false)
            => alpha ? a.r < b.r + range && a.r > b.r - range && a.g < b.g + range && a.g > b.g - range && a.b < b.b + range && a.b > b.b - range && a.a < b.a + range && a.a > b.a - range :
                a.r < b.r + range && a.r > b.r - range && a.g < b.g + range && a.g > b.g - range && a.b < b.b + range && a.b > b.b - range;

        public static bool SearchString(string searchTerm, string a) => string.IsNullOrEmpty(searchTerm) || a.ToLower().Contains(searchTerm.ToLower());

        public static bool SearchString(string searchTerm, params string[] items)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return true;

            for (int i = 0; i < items.Length; i++)
                if (items[i].ToLower().Contains(searchTerm.ToLower()))
                    return true;

            return false;
        }

        public static string ArrayToString<T>(List<T> vs)
        {
            string s = "";
            if (vs.Count > 0)
                for (int i = 0; i < vs.Count; i++)
                {
                    s += vs[i].ToString();
                    if (i != vs.Count - 1)
                        s += ", ";
                }
            return s;
        }
        
        public static string ArrayToString<T>(T[] vs)
        {
            string s = "";
            if (vs.Length > 0)
                for (int i = 0; i < vs.Length; i++)
                {
                    s += vs[i].ToString();
                    if (i != vs.Length - 1)
                        s += ", ";
                }
            return s;
        }
        
        public static string ArrayToString(params object[] vs)
        {
            string s = "";
            if (vs.Length > 0)
                for (int i = 0; i < vs.Length; i++)
                {
                    s += vs[i].ToString();
                    if (i != vs.Length - 1)
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
