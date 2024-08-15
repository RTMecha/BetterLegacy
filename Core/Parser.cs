using BetterLegacy.Core.Helpers;
using SimpleJSON;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

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
        /// Tries to parse a string into a Version.
        /// </summary>
        /// <param name="input">String to parse.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>If parse was successful, return the parsed Version. Otherwise, return assigned default value.</returns>
        public static Version TryParse(string input, Version defaultValue)
        {
            if (Version.TryParse(input, out Version result))
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

        public static Vector2 TryParse(JSONNode jn, Vector2 defaultValue)
            => new Vector2(
                jn["x"] == null ? defaultValue.x : jn["x"].AsFloat,
                jn["y"] == null ? defaultValue.y : jn["y"].AsFloat);

        public static Vector3 TryParse(JSONNode jn, Vector3 defaultValue)
            => new Vector3(
                jn["x"] == null ? defaultValue.x : jn["x"].AsFloat,
                jn["y"] == null ? defaultValue.y : jn["y"].AsFloat,
                jn["z"] == null ? defaultValue.z : jn["z"].AsFloat);

        public static Vector2Int TryParse(JSONNode jn, Vector2Int defaultValue)
            => new Vector2Int(
                jn["x"] == null ? defaultValue.x : jn["x"].AsInt,
                jn["y"] == null ? defaultValue.y : jn["y"].AsInt);

        public static Vector3Int TryParse(JSONNode jn, Vector3Int defaultValue)
            => new Vector3Int(
                jn["x"] == null ? defaultValue.x : jn["x"].AsInt,
                jn["y"] == null ? defaultValue.y : jn["y"].AsInt,
                jn["z"] == null ? defaultValue.z : jn["z"].AsInt);

        public static void ParseRectTransform(RectTransform rectTransform, JSONNode jn)
        {
            if (jn["rot"] != null)
                rectTransform.SetLocalRotationEulerZ(jn["rot"].AsFloat);

            rectTransform.anchoredPosition = jn["anc_pos"] != null && jn["anc_pos"]["x"] != null && jn["anc_pos"]["y"] != null ? jn["anc_pos"].AsVector2() : Vector2.zero;
            rectTransform.anchorMax = jn["anc_max"] != null && jn["anc_max"]["x"] != null && jn["anc_max"]["y"] != null ? jn["anc_max"].AsVector2() : new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = jn["anc_min"] != null && jn["anc_min"]["x"] != null && jn["anc_min"]["y"] != null ? jn["anc_min"].AsVector2() : new Vector2(0.5f, 0.5f);
            rectTransform.pivot = jn["pivot"] != null && jn["pivot"]["x"] != null && jn["pivot"]["y"] != null ? jn["pivot"].AsVector2() : new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = jn["size"] != null && jn["size"]["x"] != null && jn["size"]["y"] != null ? jn["size"].AsVector2() : new Vector2(100f, 100f);
        }
    }
}
