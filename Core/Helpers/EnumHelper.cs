using System;

namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Helper class for enums.
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// Gets all names of an enum.
        /// </summary>
        /// <typeparam name="T">Type of the enum.</typeparam>
        /// <returns>Returns all names of the <typeparamref name="T"/>.</returns>
        public static string[] GetNames<T>() where T : Enum => Enum.GetNames(typeof(T));

        /// <summary>
        /// Gets all values of an enum.
        /// </summary>
        /// <typeparam name="T">Type of the enum.</typeparam>
        /// <returns>Returns all values of the <typeparamref name="T"/>.</returns>
        public static T[] GetValues<T>() where T : Enum => (T[])Enum.GetValues(typeof(T));

        /// <summary>
        /// Parses a string to an enum.
        /// </summary>
        /// <typeparam name="T">Type of the enum.</typeparam>
        /// <param name="value">Input to parse.</param>
        /// <returns>Returns a parsed <typeparamref name="T"/>.</returns>
        public static T Parse<T>(string value) where T : Enum => (T)Enum.Parse(typeof(T), value);

        /// <summary>
        /// Parses a string to an enum.
        /// </summary>
        /// <typeparam name="T">Type of the enum.</typeparam>
        /// <param name="value">Input to parse.</param>
        /// <param name="ignoreCase">If letter casing matters.</param>
        /// <returns>Returns a parsed <typeparamref name="T"/>.</returns>
        public static T Parse<T>(string value, bool ignoreCase) where T : Enum => (T)Enum.Parse(typeof(T), value, ignoreCase);

        /// <summary>
        /// Checks if an enum value is defined.
        /// </summary>
        /// <typeparam name="T">Type of the enum.</typeparam>
        /// <param name="value">Enum value to check.</param>
        /// <returns>Returns true if the enum value is valid, otherwise returns false.</returns>
        public static bool IsDefined<T>(T value) where T : Enum => Enum.IsDefined(typeof(T), value);
    }
}
