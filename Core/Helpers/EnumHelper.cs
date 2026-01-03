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

        /// <summary>
        /// Tries to get an attribute from an enum value.
        /// </summary>
        /// <typeparam name="TEnum">Type of the enum.</typeparam>
        /// <typeparam name="TAttribute">Type of the attribute. Must be of type <see cref="Attribute"/>.</typeparam>
        /// <param name="value">Enum value.</param>
        /// <param name="attribute">Attribute result. Must be of type <see cref="Attribute"/>.</param>
        /// <returns>Returns true if an attribute was obtained from the enum value.</returns>
        public static bool TryGetAttribute<TEnum, TAttribute>(TEnum value, out TAttribute attribute) where TEnum : Enum where TAttribute : Attribute
        {
            var field = value.GetType().GetField(value.ToString());
            if (field != null && Attribute.GetCustomAttribute(field, typeof(TAttribute)) is TAttribute a)
            {
                attribute = a;
                return true;
            }

            attribute = default;
            return false;
        }

        /// <summary>
        /// Gets an attribute from an enum value.
        /// </summary>
        /// <typeparam name="TEnum">Type of the enum.</typeparam>
        /// <typeparam name="TAttribute">Type of the attribute. Must be of type <see cref="Attribute"/>.</typeparam>
        /// <param name="value">Enum value.</param>
        /// <returns>Returns the found attribute.</returns>
        public static Attribute GetAttribute<TEnum, TAttribute>(TEnum value) where TEnum : Enum => Attribute.GetCustomAttribute(value.GetType().GetField(value.ToString()), typeof(TAttribute));

        public static void TestEnumAttribute()
        {
            if (TryGetAttribute(TestEnum.One, out TestAttribute test))
                CoreHelper.Log($"Number is {test.number}");
        }

        public enum TestEnum
        {
            [Test(0)]
            One,
            [Test(1)]
            Two,
        }

        public class TestAttribute : Attribute
        {
            public TestAttribute() { }

            public TestAttribute(int number) => this.number = number;

            public readonly int number;
        }
    }
}
