using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Modifiers;

using Version = BetterLegacy.Core.Data.Version;

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
        /// Tries to parse a string into an enum.
        /// </summary>
        /// <typeparam name="T">Enum to return.</typeparam>
        /// <param name="input">String to parse.</param>
        /// <param name="ignoreCase">If case should be ignored or not.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>If parse was successful, return the parsed enum. Otherwise, return assigned default value.</returns>
        public static T TryParse<T>(string input, bool ignoreCase, T defaultValue) where T : struct
        {
            if (Enum.TryParse(input, ignoreCase, out T result))
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
            if (RTString.RegexMatch(str, new Regex(@"([0-9]+):([0-9]+):([0-9.]+)"), out Match match1))
            {
                var hours = float.Parse(match1.Groups[1].ToString()) * 3600f;
                var minutes = float.Parse(match1.Groups[2].ToString()) * 60f;
                var seconds = float.Parse(match1.Groups[3].ToString());

                result = hours + minutes + seconds;
                return true;
            }
            else if (RTString.RegexMatch(str, new Regex(@"([0-9]+):([0-9.]+)"), out Match match2))
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
            if (RTString.RegexMatch(str, new Regex(@"([0-9]+):([0-9]+):([0-9.]+)"), out Match match1))
            {
                var hours = float.Parse(match1.Groups[1].ToString()) * 3600f;
                var minutes = float.Parse(match1.Groups[2].ToString()) * 60f;
                var seconds = float.Parse(match1.Groups[3].ToString());

                return hours + minutes + seconds;
            }
            else if (RTString.RegexMatch(str, new Regex(@"([0-9]+):([0-9.]+)"), out Match match2))
            {
                var minutes = float.Parse(match2.Groups[1].ToString()) * 60f;
                var seconds = float.Parse(match2.Groups[2].ToString());

                return minutes + seconds;
            }

            return 0f;
        }

        public static Vector2 TryParse(JSONNode jn, Vector2 defaultValue)
            => jn == null ? defaultValue :
                            jn.IsArray ? new Vector2(jn.Count > 0 ? jn[0].AsFloat : defaultValue.x, jn.Count > 1 ? jn[1].AsFloat : defaultValue.y) :
                            new Vector2(jn["x"] == null ? defaultValue.x : jn["x"].AsFloat, jn["y"] == null ? defaultValue.y : jn["y"].AsFloat);

        public static Vector3 TryParse(JSONNode jn, Vector3 defaultValue)
            => jn == null ? defaultValue :
                            jn.IsArray ? new Vector3(jn.Count > 0 ? jn[0].AsFloat : defaultValue.x, jn.Count > 1 ? jn[1].AsFloat : defaultValue.y, jn.Count > 2 ? jn[2].AsFloat : defaultValue.z) :
                            new Vector3(jn["x"] == null ? defaultValue.x : jn["x"].AsFloat, jn["y"] == null ? defaultValue.y : jn["y"].AsFloat, jn["z"] == null ? defaultValue.z : jn["z"].AsFloat);

        public static Vector2Int TryParse(JSONNode jn, Vector2Int defaultValue)
            => jn == null ? defaultValue :
                            jn.IsArray ? new Vector2Int(jn.Count > 0 ? jn[0].AsInt : defaultValue.x, jn.Count > 1 ? jn[1].AsInt : defaultValue.y) :
                            new Vector2Int(jn["x"] == null ? defaultValue.x : jn["x"].AsInt, jn["y"] == null ? defaultValue.y : jn["y"].AsInt);

        public static Vector3Int TryParse(JSONNode jn, Vector3Int defaultValue)
            => jn == null ? defaultValue :
                            jn.IsArray ? new Vector3Int(jn.Count > 0 ? jn[0].AsInt : defaultValue.x, jn.Count > 1 ? jn[1].AsInt : defaultValue.y, jn.Count > 2 ? jn[2].AsInt : defaultValue.z) :
                            new Vector3Int(jn["x"] == null ? defaultValue.x : jn["x"].AsInt, jn["y"] == null ? defaultValue.y : jn["y"].AsInt, jn["z"] == null ? defaultValue.z : jn["z"].AsInt);

        public static List<ModifierBlock> ParseModifierBlocks(JSONNode jn, ModifierReferenceType referenceType)
        {
            var modifierBlocks = new List<ModifierBlock>();
            if (jn != null)
                for (int i = 0; i < jn.Count; i++)
                {
                    var jnModifierBlock = jn[i];
                    var modifierBlock = ModifierBlock.Parse(jnModifierBlock);
                    modifierBlock.Name = jnModifierBlock["name"];
                    modifierBlock.ReferenceType = referenceType;
                    modifierBlock.UpdateFunctions();
                    modifierBlocks.Add(modifierBlock);
                }
            return modifierBlocks;
        }

        public static JSONNode ModifierBlocksToJSON(List<ModifierBlock> modifierBlocks)
        {
            var jn = NewJSONArray();
            for (int i = 0; i < modifierBlocks.Count; i++)
            {
                var modifierBlock = modifierBlocks[i];
                var jnModifierBlock = modifierBlock.ToJSON();
                jnModifierBlock["name"] = modifierBlock.Name;
                jn[i] = jnModifierBlock;
            }
            return jn;
        }

        public static List<T> ParseObjectList<T>(JSONNode jn) where T : PAObject<T>, new()
        {
            var list = new List<T>();
            for (int i = 0; i < jn.Count; i++)
                list.Add(PAObject<T>.Parse(jn[i]));
            return list;
        }

        public static JSONNode NewJSONObject() => JSON.Parse("{}");
        public static JSONNode NewJSONArray() => JSON.Parse("[]");
    }
}
