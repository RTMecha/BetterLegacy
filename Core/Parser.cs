using BetterLegacy.Core.Helpers;
using System;
using System.Text.RegularExpressions;

namespace BetterLegacy.Core
{
    /// <summary>
    /// Parsing helper.
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// Tries to parse a string into an integer.
        /// </summary>
        /// <param name="input">String to parse.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>If parse was successful, return the parsed integer. Otherwise, return assigned default value.</returns>
		public static int TryParse(string input, int defaultValue)
        {
            if (int.TryParse(input, out int num))
                return num;
            return defaultValue;
        }

        /// <summary>
        /// Tries to parse a string into a floating point number.
        /// </summary>
        /// <param name="input">String to parse.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>If parse was successful, return the parsed floating point number. Otherwise, return assigned default value.</returns>
        public static float TryParse(string input, float defaultValue)
        {
            if (float.TryParse(input, out float num))
                return num;
            return defaultValue;
        }

        /// <summary>
        /// Tries to parse a string into a boolean.
        /// </summary>
        /// <param name="input">String to parse.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>If parse was successful, return the parsed boolean. Otherwise, return assigned default value.</returns>
        public static bool TryParse(string input, bool defaultValue)
        {
            if (bool.TryParse(input, out bool result))
                return result;
            return defaultValue;
        }

        /// <summary>
        /// Tries to parse a string into an enum.
        /// </summary>
        /// <typeparam name="T">Enum to return.</typeparam>
        /// <param name="input">String to parse.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>If parse was successful, return the parsed enum. Otherwise, return assigned default value.</returns>
        public static T TryParse<T>(string input, T defaultValue) where T : struct
        {
            if (Enum.TryParse(input, out T result))
                return result;
            return defaultValue;
        }

        /// <summary>
        /// Tries to parse a timecode string into a number.
        /// </summary>
        /// <param name="input">String to parse.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>If parse was successful, return the parsed time number. Otherwise, return assigned default value.</returns>
        public static float TryParseTimeCode(string str, float defaultValue)
        {
            if (TryParseTimeCode(str, out float result))
                return result;
            return defaultValue;
        }

        public static bool TryParseTimeCode(string str, out float result)
        {
            if (CoreHelper.RegexMatch(str, new Regex(@"([0-9]+):([0-9]+):([0-9.]+)"), out Match match1))
            {
                var hours = float.Parse(match1.Groups[1].ToString()) * 3600f;
                var minutes = float.Parse(match1.Groups[2].ToString()) * 60f;
                var seconds = float.Parse(match1.Groups[3].ToString());

                result = hours + minutes + seconds;
                return true;
            }
            else if (CoreHelper.RegexMatch(str, new Regex(@"([0-9]+):([0-9.]+)"), out Match match2))
            {
                var minutes = float.Parse(match2.Groups[1].ToString()) * 60f;
                var seconds = float.Parse(match2.Groups[2].ToString());

                result = minutes + seconds;
                return true;
            }

            result = 0f;
            return false;
        }

        public static float ParseTimeCode(string str)
        {
            if (CoreHelper.RegexMatch(str, new Regex(@"([0-9]+):([0-9]+):([0-9.]+)"), out Match match1))
            {
                var hours = float.Parse(match1.Groups[1].ToString()) * 3600f;
                var minutes = float.Parse(match1.Groups[2].ToString()) * 60f;
                var seconds = float.Parse(match1.Groups[3].ToString());

                return hours + minutes + seconds;
            }
            else if (CoreHelper.RegexMatch(str, new Regex(@"([0-9]+):([0-9.]+)"), out Match match2))
            {
                var minutes = float.Parse(match2.Groups[1].ToString()) * 60f;
                var seconds = float.Parse(match2.Groups[2].ToString());

                return minutes + seconds;
            }

            return 0f;
        }
    }
}
